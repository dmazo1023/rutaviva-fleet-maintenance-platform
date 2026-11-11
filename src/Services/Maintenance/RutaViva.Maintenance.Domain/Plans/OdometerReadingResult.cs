namespace RutaViva.Maintenance.Domain.Plans;

public sealed record OdometerReadingResult
{
    private OdometerReadingResult(
        OdometerReadingOutcome outcome,
        OdometerReadingIgnoredReason? ignoredReason,
        ReevaluationOutcome evaluation)
    {
        Outcome = outcome;
        IgnoredReason = ignoredReason;
        Evaluation = evaluation;
    }

    public OdometerReadingOutcome Outcome { get; }

    public OdometerReadingIgnoredReason? IgnoredReason { get; }

    public ReevaluationOutcome Evaluation { get; }

    public bool IsAccepted => Outcome == OdometerReadingOutcome.Accepted;

    public static OdometerReadingResult Accepted(ReevaluationOutcome evaluation) =>
        new(OdometerReadingOutcome.Accepted, null, evaluation);

    public static OdometerReadingResult Ignored(OdometerReadingIgnoredReason reason) =>
        new(OdometerReadingOutcome.Ignored, reason, ReevaluationOutcome.None);
}
