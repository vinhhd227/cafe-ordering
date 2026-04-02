import api from './axios'

export const getExpenses = ({ category, paymentMethod, dateFrom, dateTo, page = 1, pageSize = 20 } = {}) =>
  api.get('/admin/expenses', {
    params: { category, paymentMethod, dateFrom, dateTo, page, pageSize },
  })

export const getExpense = (id) =>
  api.get(`/admin/expenses/${id}`)

export const createExpense = (data) =>
  api.post('/admin/expenses', data)

export const updateExpense = (id, data) =>
  api.put(`/admin/expenses/${id}`, data)

export const deleteExpense = (id) =>
  api.delete(`/admin/expenses/${id}`)

export const getExpenseSummary = ({ dateFrom, dateTo } = {}) =>
  api.get('/admin/expenses/summary', {
    params: { dateFrom, dateTo },
  })
