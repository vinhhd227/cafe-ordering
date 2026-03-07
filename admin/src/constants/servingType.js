export const SERVING_TYPE = Object.freeze({
  DINE_IN:  false,
  TAKEAWAY: true,
});

export const SERVING_TYPE_MAP = Object.freeze({
  DINE_IN:  { label: 'Dine-in',  icon: 'tdesign:drink-filled' },
  TAKEAWAY: { label: 'Takeaway', icon: 'streamline:coffee-takeaway-cup-remix' },
});

/** Array ready for v-for */
export const SERVING_TYPE_OPTIONS = Object.freeze([
  { value: SERVING_TYPE.DINE_IN,  ...SERVING_TYPE_MAP.DINE_IN },
  { value: SERVING_TYPE.TAKEAWAY, ...SERVING_TYPE_MAP.TAKEAWAY },
]);
