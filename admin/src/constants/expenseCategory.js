export const EXPENSE_CATEGORY = Object.freeze({
  INGREDIENT: 'INGREDIENT',
  SUPPLY:     'SUPPLY',
  EQUIPMENT:  'EQUIPMENT',
  OTHER:      'OTHER',
})

export const EXPENSE_CATEGORY_MAP = Object.freeze({
  [EXPENSE_CATEGORY.INGREDIENT]: { label: 'Ingredients', icon: 'ph:drop-bold',       severity: 'success'  },
  [EXPENSE_CATEGORY.SUPPLY]:     { label: 'Supplies',    icon: 'ph:package-bold',    severity: 'info'     },
  [EXPENSE_CATEGORY.EQUIPMENT]:  { label: 'Equipment',   icon: 'ph:wrench-bold',     severity: 'warn'     },
  [EXPENSE_CATEGORY.OTHER]:      { label: 'Other',       icon: 'ph:dots-three-bold', severity: 'secondary' },
})

/** Array ready for v-for / prime-select options */
export const EXPENSE_CATEGORY_OPTIONS = Object.freeze(
  Object.values(EXPENSE_CATEGORY).map((v) => ({ value: v, ...EXPENSE_CATEGORY_MAP[v] })),
)
