import { TeamOutlined } from '@ant-design/icons'
import { Layout, Menu, Typography } from 'antd'
import { Outlet } from 'react-router'

const { Header, Content } = Layout

export function AppLayout() {
  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Header style={{ display: 'flex', alignItems: 'center', gap: 24 }}>
        <Typography.Title level={4} style={{ color: '#fff', margin: 0, whiteSpace: 'nowrap' }}>
          OpenSpec
        </Typography.Title>
        <Menu
          theme="dark"
          mode="horizontal"
          selectedKeys={['users']}
          items={[{ key: 'users', icon: <TeamOutlined />, label: 'Người dùng' }]}
          style={{ flex: 1, minWidth: 0 }}
        />
      </Header>
      <Content style={{ padding: 24, maxWidth: 1200, width: '100%', margin: '0 auto' }}>
        <Outlet />
      </Content>
    </Layout>
  )
}
