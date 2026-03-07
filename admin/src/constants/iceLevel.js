export const ICE_LEVEL = Object.freeze({
  LESS:   'LESS',
  NORMAL: 'NORMAL',
  MORE:   'MORE',
});

export const ICE_LEVEL_MAP = Object.freeze({
  [ICE_LEVEL.LESS]:   { label: 'Less ice',   icon: 'game-icons:ice-cube' },
  [ICE_LEVEL.NORMAL]: { label: 'Normal ice', icon: 'flowbite:cubes-stacked-solid' },
  [ICE_LEVEL.MORE]:   { label: 'More ice',   icon: 'fa6-solid:cubes-stacked' },
});

/** Array ready for v-for */
export const ICE_LEVEL_OPTIONS = Object.freeze(
  Object.values(ICE_LEVEL).map((v) => ({ value: v, ...ICE_LEVEL_MAP[v] })),
);
