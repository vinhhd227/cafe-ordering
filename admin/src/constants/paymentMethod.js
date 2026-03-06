export const PAYMENT_METHOD = Object.freeze({
  UNKNOWN: 'UNKNOWN',
  CASH: 'CASH',
  BANK_TRANSFER: 'BANK_TRANSFER',
});

export const PAYMENT_METHOD_MAP = Object.freeze({
  [PAYMENT_METHOD.UNKNOWN]:       { label: 'Unknown',       icon: 'ph:question-bold' },
  [PAYMENT_METHOD.CASH]:          { label: 'Cash',          icon: 'ph:money-bold' },
  [PAYMENT_METHOD.BANK_TRANSFER]: { label: 'Bank Transfer', icon: 'ph:bank-bold' },
});
