import { ShoppingOutlined, TeamOutlined } from '@ant-design/icons'
import { Layout, Menu, Typography } from 'antd'
import { Outlet, useLocation, useNavigate } from 'react-router'

const { Header, Content } = Layout

const menuItems = [
  { key: '/', icon: <TeamOutlined />, label: 'Người dùng' },
  { key: '/products', icon: <ShoppingOutlined />, label: 'Sản phẩm' },
]

export function AppLayout() {
  const { pathname } = useLocation()
  const navigate = useNavigate()

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Header style={{ display: 'flex', alignItems: 'center', gap: 24 }}>
        <Typography.Title level={4} style={{ color: '#fff', margin: 0, whiteSpace: 'nowrap' }}>
          OpenSpec
        </Typography.Title>
        <Menu
          theme="dark"
          mode="horizontal"
          selectedKeys={[pathname.startsWith('/products') ? '/products' : '/']}
          items={menuItems}
          onClick={({ key }) => navigate(key)}
          style={{ flex: 1, minWidth: 0 }}
        />
      </Header>
      <Content style={{ padding: 24, maxWidth: 1200, width: '100%', margin: '0 auto' }}>
        <Outlet />
      </Content>
    </Layout>
  )
}
