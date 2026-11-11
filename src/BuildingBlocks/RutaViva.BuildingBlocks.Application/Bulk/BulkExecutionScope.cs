namespace RutaViva.BuildingBlocks.Application.Bulk;

public static class BulkExecutionScope
{
    public static IBulkExecutionScope For(bool dryRun) =>
        dryRun ? new DryRunBulkExecutionScope() : new CommittingBulkExecutionScope();
}
