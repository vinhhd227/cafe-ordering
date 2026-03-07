export const PAYMENT_STATUS = Object.freeze({
  UNPAID:   'UNPAID',
  PAID:     'PAID',
  REFUNDED: 'REFUNDED',
  VOIDED:   'VOIDED',
});

export const PAYMENT_STATUS_MAP = Object.freeze({
  [PAYMENT_STATUS.UNPAID]:   { label: 'Unpaid',   severity: 'warn',      icon: 'ph:clock-bold' },
  [PAYMENT_STATUS.PAID]:     { label: 'Paid',      severity: 'success',   icon: 'ph:check-circle-bold' },
  [PAYMENT_STATUS.REFUNDED]: { label: 'Refunded',  severity: 'secondary', icon: 'ph:arrow-counter-clockwise-bold' },
  [PAYMENT_STATUS.VOIDED]:   { label: 'Voided',    severity: 'danger',    icon: 'ph:x-circle-bold' },
});

/** Array ready for v-for */
export const PAYMENT_STATUS_OPTIONS = Object.freeze(
  Object.values(PAYMENT_STATUS).map((v) => ({ value: v, ...PAYMENT_STATUS_MAP[v] })),
);
