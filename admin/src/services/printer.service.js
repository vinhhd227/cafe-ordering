import api from './axios'

export const listPrinters      = ()       => api.get('/admin/printers')
export const createPrinter     = (data)   => api.post('/admin/printers', data)
export const updatePrinter     = (id, data) => api.put(`/admin/printers/${id}`, data)
export const deletePrinter     = (id)     => api.delete(`/admin/printers/${id}`)
export const setDefaultPrinter = (id)     => api.put(`/admin/printers/${id}/set-default`, {})
export const testPrinter       = (id)     => api.post(`/admin/printers/${id}/test`, {})
export const printDrinkLabels  = (data)   => api.post('/admin/print/drink-labels', data)
