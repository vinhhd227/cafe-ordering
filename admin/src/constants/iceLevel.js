export const ICE_LEVEL = Object.freeze({
  LESS: 'LESS',
  NORMAL: 'NORMAL',
  MORE: 'MORE',
});

export const ICE_LEVEL_MAP = Object.freeze({
  [ICE_LEVEL.LESS]:   { label: 'Less ice' },
  [ICE_LEVEL.NORMAL]: { label: 'Normal ice' },
  [ICE_LEVEL.MORE]:   { label: 'More ice' },
});
