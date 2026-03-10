export const EXPENSE_PAYMENT_METHOD = Object.freeze({
  CASH:          'CASH',
  BANK_TRANSFER: 'BANK_TRANSFER',
})

export const EXPENSE_PAYMENT_METHOD_MAP = Object.freeze({
  [EXPENSE_PAYMENT_METHOD.CASH]:          { label: 'Cash',          icon: 'ph:money-bold',        severity: 'success' },
  [EXPENSE_PAYMENT_METHOD.BANK_TRANSFER]: { label: 'Bank transfer', icon: 'ph:bank-bold',         severity: 'info' },
})

export const EXPENSE_PAYMENT_METHOD_OPTIONS = Object.values(EXPENSE_PAYMENT_METHOD).map((v) => ({
  value: v,
  label: EXPENSE_PAYMENT_METHOD_MAP[v].label,
  icon:  EXPENSE_PAYMENT_METHOD_MAP[v].icon,
}))
