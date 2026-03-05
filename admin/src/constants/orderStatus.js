export const ORDER_STATUS = Object.freeze({
    PENDING: 'PENDING',
    PROCESSING: 'PROCESSING',
    COMPLETED: 'COMPLETED',
    CANCELLED: 'CANCELLED'
});

export const ORDER_STATUS_MAP = Object.freeze({
    [ORDER_STATUS.PENDING]: { label: 'Pending', severity: 'warn', icon: 'pi pi-clock' },
    [ORDER_STATUS.PROCESSING]: { label: 'Processing', severity: 'info', icon: 'pi pi-truck' },
    [ORDER_STATUS.COMPLETED]: { label: 'Completed', severity: 'success', icon: 'pi pi-check' },
    [ORDER_STATUS.CANCELLED]: { label: 'Cancelled', severity: 'danger', icon: 'pi pi-times' },
});