export const CODE_VISIBILITY = {
  PUBLIC:  'PUBLIC',
  PRIVATE: 'PRIVATE',
};

export const CODE_VISIBILITY_MAP = {
  PUBLIC:  { label: 'Public',  icon: 'ph:eye-bold',        severity: 'success' },
  PRIVATE: { label: 'Private', icon: 'ph:eye-slash-bold',  severity: 'warn'    },
};

export const CODE_VISIBILITY_OPTIONS = Object.entries(CODE_VISIBILITY_MAP).map(
  ([value, { label, icon }]) => ({ value, label, icon }),
);
