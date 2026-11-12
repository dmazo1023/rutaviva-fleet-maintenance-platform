const ISO_DATE_PATTERN: RegExp = /^(\d{4})-(\d{2})-(\d{2})/;

export function formatAlertDate(raw: string): string {
  const match: RegExpMatchArray | null = ISO_DATE_PATTERN.exec(raw);
  if (!match) {
    return raw;
  }
  return `${match[3]}/${match[2]}/${match[1]}`;
}
