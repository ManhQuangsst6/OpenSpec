import { useQuery } from '@tanstack/react-query'
import { apiClient } from '@/api/client'

export function useUsersPing() {
  return useQuery({
    queryKey: ['users', 'ping'],
    queryFn: async () => {
      const { data, error } = await apiClient.GET('/api/users/ping')
      if (error) throw new Error('Không gọi được API users')
      return data
    },
  })
}
