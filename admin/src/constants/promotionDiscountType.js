export const PROMOTION_DISCOUNT_TYPE = Object.freeze({
  PERCENTAGE: 'PERCENTAGE',
  FIXED: 'FIXED',
  BUY_X_GET_Y: 'BUY_X_GET_Y',
})

export const PROMOTION_DISCOUNT_TYPE_MAP = Object.freeze({
  PERCENTAGE: { label: 'Percentage',  icon: 'ph:percent-bold',                severity: 'info'    },
  FIXED:      { label: 'Fixed amount', icon: 'ph:currency-circle-dollar-bold', severity: 'success' },
  BUY_X_GET_Y:{ label: 'Buy X Get Y', icon: 'ph:gift-bold',                   severity: 'warning' },
})

export const PROMOTION_DISCOUNT_TYPE_OPTIONS = Object.values(PROMOTION_DISCOUNT_TYPE).map((v) => ({
  value: v,
  label: PROMOTION_DISCOUNT_TYPE_MAP[v].label,
  icon:  PROMOTION_DISCOUNT_TYPE_MAP[v].icon,
}))
