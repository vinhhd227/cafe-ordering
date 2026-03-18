import api from './axios'

// POST /api/admin/uploads/image  → { url: string }
// Accepts a File object, returns the public URL of the uploaded image.
export const uploadImage = (file) => {
  const formData = new FormData()
  formData.append('file', file)
  return api.post('/admin/uploads/image', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  })
}
