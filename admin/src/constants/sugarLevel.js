export const SUGAR_LEVEL = Object.freeze({
  LESS:   'LESS',
  NORMAL: 'NORMAL',
  MORE:   'MORE',
});

export const SUGAR_LEVEL_MAP = Object.freeze({
  [SUGAR_LEVEL.LESS]:   { label: 'Less sugar',   icon: 'ph:drop-bold' },
  [SUGAR_LEVEL.NORMAL]: { label: 'Normal sugar',  icon: 'ph:drop-half-bold' },
  [SUGAR_LEVEL.MORE]:   { label: 'More sugar',    icon: 'ph:drop-fill' },
});

/** Array ready for v-for */
export const SUGAR_LEVEL_OPTIONS = Object.freeze(
  Object.values(SUGAR_LEVEL).map((v) => ({ value: v, ...SUGAR_LEVEL_MAP[v] })),
);
