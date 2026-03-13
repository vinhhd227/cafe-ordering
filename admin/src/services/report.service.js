import api from './axios'

export const getOrdersSummary = ({ dateFrom, dateTo } = {}) =>
  api.get('/admin/reports/orders-summary', { params: { dateFrom, dateTo } })
