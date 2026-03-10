export const STACK_POLICY = Object.freeze({
  EXCLUSIVE: 'EXCLUSIVE',
  STACKABLE: 'STACKABLE',
})

export const STACK_POLICY_MAP = Object.freeze({
  EXCLUSIVE: { label: 'Exclusive', icon: 'ph:prohibit-bold', severity: 'danger'  },
  STACKABLE:  { label: 'Stackable', icon: 'ph:stack-bold',   severity: 'success' },
})

export const STACK_POLICY_OPTIONS = Object.values(STACK_POLICY).map((v) => ({
  value: v,
  label: STACK_POLICY_MAP[v].label,
  icon:  STACK_POLICY_MAP[v].icon,
}))
