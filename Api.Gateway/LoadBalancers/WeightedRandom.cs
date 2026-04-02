using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace Api.Gateway.LoadBalancers;

/// <summary>
/// Балансировщик нагрузки, выбирающий downstream-сервис случайным образом с учётом весов.
/// Чем больше вес у сервиса, тем выше вероятность его выбора.
/// Веса задаются в конфигурации (<c>WeightedRandom:Weights</c>) и применяются к сервисам в порядке их перечисления в маршруте Ocelot.
/// </summary>
/// <param name="services">Фабричная функция получения списка downstream-сервисов.</param>
/// <param name="configuration">Конфигурация приложения с секцией весов.</param>
public class WeightedRandom(Func<Task<List<Service>>> services, IConfiguration configuration) : ILoadBalancer
{
    private static readonly object _locker = new();
    private readonly int[] _weights = configuration
        .GetSection("WeightedRandom:Weights")
        .Get<int[]>() ?? [1, 1, 1, 1, 1];
    private readonly Random _random = new(111);

    public string Type => nameof(WeightedRandom);

    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var serviceList = await services();

        if (serviceList.Count == 0)
            throw new InvalidOperationException("Нет доступных сервисов");

        lock (_locker)
        {
            var totalWeight = 0;
            for (var i = 0; i < serviceList.Count; i++)
                totalWeight += i < _weights.Length ? _weights[i] : 1;

            var threshold = _random.Next(totalWeight);
            var cumulative = 0;
            for (var i = 0; i < serviceList.Count; i++)
            {
                cumulative += i < _weights.Length ? _weights[i] : 1;
                if (threshold < cumulative)
                    return new OkResponse<ServiceHostAndPort>(serviceList[i].HostAndPort);
            }

            return new OkResponse<ServiceHostAndPort>(serviceList[^1].HostAndPort);
        }
    }

    public void Release(ServiceHostAndPort hostAndPort) { }
}
