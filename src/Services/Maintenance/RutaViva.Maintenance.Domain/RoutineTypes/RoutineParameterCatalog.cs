using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.RoutineTypes.ValueObjects;

namespace RutaViva.Maintenance.Domain.RoutineTypes;

public sealed class RoutineParameterCatalog
{
    private readonly Dictionary<RoutineTypeId, RoutineTypeParameters> _byId;
    private readonly Dictionary<string, RoutineTypeParameters> _byCode;

    public RoutineParameterCatalog(IEnumerable<KeyValuePair<RoutineTypeId, RoutineTypeParameters>> entries)
    {
        _byId = new Dictionary<RoutineTypeId, RoutineTypeParameters>(entries);
        _byCode = _byId.Values
            .DistinctBy(parameters => parameters.Code.Value)
            .ToDictionary(parameters => parameters.Code.Value);
    }

    public int Count => _byId.Count;

    public static RoutineParameterCatalog From(IEnumerable<RoutineType> routineTypes) =>
        new(routineTypes.Select(routineType => KeyValuePair.Create(routineType.Id, routineType.ToParameters())));

    public Result<RoutineTypeParameters> Find(RoutineTypeId id)
    {
        if (!_byId.TryGetValue(id, out RoutineTypeParameters? parameters))
        {
            return MaintenanceErrors.RoutineTypeNotFound;
        }

        return parameters;
    }

    public Result<RoutineTypeParameters> Find(RoutineTypeCode code)
    {
        if (!_byCode.TryGetValue(code.Value, out RoutineTypeParameters? parameters))
        {
            return MaintenanceErrors.RoutineTypeNotFound;
        }

        return parameters;
    }

    public Result<RoutineTypeParameters> FindByCode(RoutineTypeCode code) => Find(code);
}
