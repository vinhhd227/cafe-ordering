export const DRINK_TEMPERATURE = Object.freeze({
    HOT: 'HOT',
    COLD: 'COLD'
});

export const DRINK_TEMPERATURE_MAP = Object.freeze({
    [DRINK_TEMPERATURE.HOT]: { label: 'Hot', severity: 'warn', icon: 'ph:thermometer-hot-light' },
    [DRINK_TEMPERATURE.COLD]: { label: 'Cold', severity: 'info', icon: 'ph:thermometer-cold-light' },
});