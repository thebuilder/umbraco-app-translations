import type { MessageFormat, TranslationOutputFormat } from "../../api/generated/models.js";

export const messageFormatLabel = (format: MessageFormat): string => {
  switch (format) {
    case "Icu":
      return "ICU messages";
    case "I18NextV4":
      return "i18next JSON v4";
    case "PlainText":
      return "Plain text";
    // A server newer than this bundle can name a format this build has never heard of. Its own
    // name reads better in the UI than the blank a missing branch would leave.
    default:
      return format;
  }
};

export const messageFormatOptions = (selected: MessageFormat) =>
  (["Icu", "I18NextV4", "PlainText"] as const).map((value) => ({
    name: messageFormatLabel(value),
    value,
    selected: value === selected,
  }));

export const deliveryFormatLabel = (format: TranslationOutputFormat): string => {
  switch (format) {
    case "icu":
      return "ICU MessageFormat";
    case "i18next":
      return "i18next JSON v4";
    default:
      return format;
  }
};

/** The schedules the source editor offers, in minutes. */
const SYNC_INTERVALS = [60, 360, 720, 1440, 10_080] as const;

const MINUTES_PER_HOUR = 60;
const MINUTES_PER_DAY = 1440;
const MINUTES_PER_WEEK = 10_080;

export const syncIntervalLabel = (minutes: number | null | undefined): string => {
  if (!minutes) {
    return "Only when synced by hand or by a deployment";
  }
  if (minutes === MINUTES_PER_HOUR) {
    return "Every hour";
  }
  if (minutes === MINUTES_PER_DAY) {
    return "Every day";
  }
  if (minutes === MINUTES_PER_WEEK) {
    return "Every week";
  }
  return minutes % MINUTES_PER_HOUR === 0
    ? `Every ${minutes / MINUTES_PER_HOUR} hours`
    : `Every ${minutes} minutes`;
};

/**
 * The fixed choices, plus whatever the source already has. A schedule set through the API to
 * something the list does not offer would otherwise show as the first option and be lost on save.
 */
export const syncIntervalOptions = (selected: number | null | undefined) => {
  const values: (number | null)[] = [null, ...SYNC_INTERVALS];
  if (selected && !values.includes(selected)) {
    values.push(selected);
  }
  return values.map((value) => ({
    name: syncIntervalLabel(value),
    value: value === null ? "" : String(value),
    selected: (value ?? null) === (selected ?? null),
  }));
};

export const parseSyncInterval = (value: string): number | null => {
  const minutes = Number(value);
  return value !== "" && Number.isInteger(minutes) && minutes > 0 ? minutes : null;
};
