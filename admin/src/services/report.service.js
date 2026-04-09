import api from './axios'

export const getOrdersSummary = ({ dateFrom, dateTo } = {}) =>
  api.get('/admin/reports/orders-summary', { params: { dateFrom, dateTo } })

export const getMonthlyComparison = ({ year, month }) =>
  api.get('/admin/reports/monthly-comparison', { params: { year, month } })

export const getDailyReport = ({ date }) =>
  api.get('/admin/reports/daily', { params: { date } })

export const getDailyOrders = ({ date }) =>
  api.get('/admin/reports/daily/orders', { params: { date } })
