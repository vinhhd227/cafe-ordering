export const PROMOTION_SCOPE = Object.freeze({
  ORDER:    'ORDER',
  PRODUCT:  'PRODUCT',
  CATEGORY: 'CATEGORY',
})

export const PROMOTION_SCOPE_MAP = Object.freeze({
  ORDER:    { label: 'Entire order',     icon: 'ph:receipt-bold',  severity: 'info'    },
  PRODUCT:  { label: 'Specific product', icon: 'ph:package-bold',  severity: 'success' },
  CATEGORY: { label: 'Category',         icon: 'ph:tag-bold',      severity: 'warning' },
})

export const PROMOTION_SCOPE_OPTIONS = Object.values(PROMOTION_SCOPE).map((v) => ({
  value: v,
  label: PROMOTION_SCOPE_MAP[v].label,
  icon:  PROMOTION_SCOPE_MAP[v].icon,
}))
