namespace RutaViva.Maintenance.Application.Abstractions;

public interface IOdometerReadingLog
{
    void Append(OdometerReadingLogEntry entry);
}
