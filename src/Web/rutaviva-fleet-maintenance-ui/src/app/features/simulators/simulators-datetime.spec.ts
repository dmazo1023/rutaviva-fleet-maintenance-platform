import { currentDateInput, currentDateTimeLocal, toBogotaIso } from './simulators-datetime';

describe('simulators-datetime', () => {
  it('should convert a datetime-local value to ISO with the Bogota offset', () => {
    expect(toBogotaIso('2026-11-12T09:00')).toBe('2026-11-12T09:00:00-05:00');
  });

  it('should keep seconds when the value already has them', () => {
    expect(toBogotaIso('2026-11-12T09:00:30')).toBe('2026-11-12T09:00:30-05:00');
  });

  it('should format the current moment with the shapes expected by the inputs', () => {
    const now: Date = new Date(2026, 10, 12, 9, 5);
    expect(currentDateTimeLocal(now)).toBe('2026-11-12T09:05');
    expect(currentDateInput(now)).toBe('2026-11-12');
  });
});
