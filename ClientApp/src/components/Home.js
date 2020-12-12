import React, { Component } from 'react';
import { Card, Form, Drawer, Input, Button, Alert, Table, Space, Modal, Tag, Spin, Popconfirm, message, Switch, List } from 'antd';
import { reqComparaData, reqCleanTask, reqComparaTask, reqLogin, reqTaskStatus, reqTaskDetailStatus, reqReportResult, reqDeleteTaske, reqHistoryTask, reqAwakeTask } from '../Utils/ajax';
import { formatJson } from '../Utils/jsonView';
import './Home.css';
const { TextArea } = Input;
export class Home extends Component {

  constructor(props) {
    super(props);
    this.state =
    {
      response: "",
      visible: false,
      issucceed: false,
      columns: this.columns,
      data: [],
      detailData: [],
      detailColumns: this.detailColumns,
      currentTaskID: null,
      reportResultVisible: false,
      reportResult: "",
      downloadVisible: false,
      selectedRowKeys: [], // Check here to configure the default column
      comparaVisible: false,
      shiftFlag: false,
      diffResult: [],
      drawerTitle: "",
      showForm: false,
      showDataDifference: false,
      differentData: [],
      currentPageNum:1,
      searchName:"",
      planName:""
    }
  }
  componentDidMount() {
    this.interval = setInterval
      (
        async () => {
          const data = await reqTaskStatus();
          var count = 0;
          var unExecuteCount = 0;
          data.forEach(element => {
            if (element.status == 3) {
              count++;
            }
            if (element.status == 2) {
              unExecuteCount++;
            }
          });
          if (count < 5 && unExecuteCount > 0) {
            //激活任务
            reqAwakeTask();
          }
          this.setState({ data: data })
        }
        , 1000
      )

  }
  componentWillUnmount() {
    clearInterval(this.interval);
  }
  render() {
    const { selectedRowKeys } = this.state;
    const rowSelection = {
      selectedRowKeys,
      onChange: this.onSelectChange
    };
    return (
      <div>
        {this.state.visible ? <Alert message={this.state.response} type={this.state.issucceed ? "success" : "error"} /> : <div></div>}
        <Drawer
          title="创建一个新任务"
          width={720}
          onClose={this.closeShowForm}
          visible={this.state.showForm}
          bodyStyle={{ paddingBottom: 80 }}
          placement="left"
        >
          <Form
            name="basic"
            initialValues={{
              Password: "zc111111",
              ServerURL: "http://testi-t.chanjet.com/TPlus/api/v1/",
              UserName: "13161351931",
              AccountNum: "1405",
              LoopTimes: 1,
              PageSize: 100,
            }}
            onFinish={this.onFinish}
            onFinishFailed={this.onFinishFailed}
          >
            <Form.Item
              label="服务器地址"
              name="ServerURL"
              rules={[
                {
                  required: true,
                  message: '请输入服务器地址!',
                },
              ]}
            >
              <Input />
            </Form.Item>
            <Form.Item
              label="用户名"
              name="UserName"
              rules={[
                {
                  required: true,
                  message: '请输入用户名!',
                },
              ]}
            >
              <Input />
            </Form.Item>

            <Form.Item
              label="密码"
              name="Password"
              rules={[
                {
                  required: true,
                  message: '请输入密码!',
                },
              ]}
            >
              <Input.Password />
            </Form.Item>
            <Form.Item
              label="账套号"
              name="AccountNum"
              rules={[
                {
                  required: true,
                  message: '请输入账套号!',
                },
              ]}
            >
              <Input />
            </Form.Item>
            <Form.Item
              label="循环次数"
              name="LoopTimes"
              rules={[
                {
                  required: false,
                  message: 'Please input your username!',
                },
              ]}
            >
              <Input />
            </Form.Item>
            <Form.Item
              label="每次查询数据量"
              name="PageSize"
              rules={[
                {
                  required: true,
                  message: '请输入每次查询的数据量!',
                },
              ]}
            >
              <Input />
            </Form.Item>
            <Form.Item
              label="是否执行系统方案"
              name='IsSystem'
            >
              <Switch />
            </Form.Item>
            <Form.Item
              label="是否获取全部数据"
              name='IsGetAllData'
            >
              <Switch />
            </Form.Item>
            <Form.Item>
              <Button className='Start' type="primary" htmlType="submit">
                开始
        </Button>
            </Form.Item>
          </Form>
        </Drawer>

        <Modal
          title="数据差异"
          visible={this.state.showDataDifference}
          onOk={this.handleDataOk}
          onCancel={this.handleDataCancel}
          width={1200}
          okText="下一页"
        >
          <List
            itemLayout='vertical'
            grid={{ gutter: 8, column: 2 }}
            dataSource={this.state.differentData}

            renderItem={item => (
              <List.Item>
                <div dangerouslySetInnerHTML={{ __html: item }}></div>
              </List.Item>
            )}
          />

        </Modal>
        <Button onClick={this.showForm}>打开任务发射列表</Button>
        <Button className='history_btn' onClick={this.getHistoryTask}>加载历史记录</Button>
        <Button className='compara_btn' onClick={this.comparaTask}>比较任务耗时</Button>
        <Button className='clean_btn' onClick={this.cleanTask}>清空当前列表</Button>
        <Modal
          title="响应信息"
          visible={this.state.reportResultVisible}
          onOk={this.handleOk}
          onCancel={this.handleCancel}
          
        >
          <TextArea
            autoSize={{ minRows: 2, maxRows: 500 }}
            value={this.state.reportResultVisible == true ? formatJson(this.state.reportResult) : ""}
          />
        </Modal>
        <Drawer
          width={980}
          placement="right"
          closable={false}
          onClose={this.onDrawerClose}
          visible={this.state.comparaVisible}
        >
          <Card title={this.state.drawerTitle} extra={

            <Button onClick={this.shiftDiffTask}>{this.state.shiftFlag == false ? '切换A|B' : '切换B|A'}</Button>

          }>
            <Table columns={this.diffTask} dataSource={this.state.diffResult} />
          </Card>

        </Drawer>
        <Table
          rowSelection={rowSelection}
          rowKey={record => record.id}
          columns={this.state.columns} dataSource={this.state.data} scroll={{ y: 520 }} />
        <Table columns={this.state.detailColumns} dataSource={this.state.detailData} pagination={{ pageSize: 50 }} scroll={{ y: 240 }} />
      </div>
    );
  }
  onFinish = async (values) => {
    this.setState({
      isclicked: true
    })
    var result = await reqLogin(values);
    var issucceed = true;
    if (result.indexOf("Error") !== -1) {
      // 包含 
      issucceed = false;
    }
    this.setState(
      {
        response: result,
        visible: true,
        issucceed: issucceed
      }
    );
    console.log(result);
  };
  comparaData = async (searchName, leftPlanName,index) => {

    if(index==0)
    {
      index =1;
    }
    var request = this.state.selectedRowKeys;
    if (request.length !== 2) {
      message.info("请勾选两项进行比较!");
      return;
    }
    debugger;
    var data = await reqComparaData(request[0], request[1], searchName, leftPlanName, index);
    var result_index = index+1;
    if(data.length==0)
    {
      message.info("页数到头了!");
      result_index = 0;
    }

    this.setState({
      showDataDifference: true,
      currentPageNum:result_index,
      differentData:data,
      searchName:searchName,
      planName:leftPlanName
    });
  }
  handleDataCancel = () => {
    this.setState({
      showDataDifference: false
    });
  }
  handleDataOk = () => {
    this.comparaData(this.state.searchName,this.state.planName,this.state.currentPageNum);
  }
  showForm = () => {
    this.setState({ showForm: true });
  }
  closeShowForm = () => {
    this.setState({ showForm: false });
  }
  shiftDiffTask = async () => {
    var data = this.state.selectedRowKeys;
    var result = [];
    var temp = [];
    var drawerTitle = "";
    if (this.state.shiftFlag == false) {
      drawerTitle = data[1] + "|" + data[0];
      temp = await reqComparaTask(data[1], data[0]);
      temp.forEach(e => {
        result.push(e);
      });
      this.setState({
        shiftFlag: true
      });
    } else {
      drawerTitle = data[0] + "|" + data[1];
      temp = await reqComparaTask(data[0], data[1]);
      temp.forEach(e => {
        result.push(e);
      });
      this.setState({
        shiftFlag: false
      });
    }

    this.setState({
      drawerTitle: drawerTitle,
      diffResult: result
    });
  }
  onFinishFailed = (errorInfo) => {
    console.log('Failed:', errorInfo);
  };
  getTaskDetail = async (id) => {
    var result = await reqTaskDetailStatus(id);
    this.setState(
      {
        detailData: result,
        currentTaskID: id
      }
    );
    console.log(result);
  }
  getReportResult = async (id) => {
    var result = await reqReportResult(this.state.currentTaskID, id);
    var str = JSON.stringify(result);
    this.setState(
      {
        reportResult: str,
        reportResultVisible: true
      }
    );
  }

  deleteTask = async (id) => {
    var result = await reqDeleteTaske(id);
    if (result) {
      message.success("成功删除!");
    } else {
      message.error("删除失败!");
    }
  }

  comparaTask = async () => {
    var request = this.state.selectedRowKeys;
    if (request.length !== 2) {
      message.info("请勾选两项进行比较!");
      return;
    }
    var data = await reqComparaTask(request[0], request[1]);
    var drawerTitle = "";
    drawerTitle = request[0] + "|" + request[1];
    var result = [];
    data.forEach(e => {
      result.push(e);
    });
    this.setState({
      diffResult: result,
      comparaVisible: true,
      drawerTitle: drawerTitle,
    });
  }
  getFileAddress = (txt) => {
    message.success(txt);
  }
  handleOk = e => {
    this.setState({
      reportResultVisible: false,
    });
  };

  handleCancel = e => {
    this.setState({
      reportResultVisible: false,
    });
  };

  getHistoryTask = () => {
    reqHistoryTask();
  }
  onSelectChange = selectedRowKeys => {
    console.log('selectedRowKeys changed: ', selectedRowKeys);
    this.setState({ selectedRowKeys });
  };
  onDrawerClose = () => {
    this.setState({
      comparaVisible: false
    })
  }

  cleanTask = () => {
    reqCleanTask();
  }

  columns = [
    {
      title: '服务器地址',
      dataIndex: 'serverURL',
      key: 'serverURL',
    },
    {
      title: '账套',
      dataIndex: 'accountNum',
      key: 'accountNum',
    },
    {
      title: '用户',
      dataIndex: 'userName',
      key: 'userName',
    },
    {
      title: '执行时间',
      dataIndex: 'startTime',
      key: 'startTime',
    },
    {
      title: '执行次数',
      dataIndex: 'loopTimes',
      key: 'loopTimes',
    },
    {
      title: '状态',
      key: 'runningStatus',
      render: (text, item) => (
        <Space size="middle">
          <Button type='link' onClick={() => { this.getTaskDetail(item.id) }}>查看状态</Button>
        </Space>
      ),
    },
    {
      title: '执行状态',
      key: 'result',
      width: 150,
      render: (text, record) => (
        <Space size="middle">
          {
            record.status == 0 ?
              <Button onClick={() => { this.getFileAddress(record.fileAddress) }}>文件地址</Button>
              : record.status == 1 ? <Button onClick={() => { message.error(record.errorMessage) }} ><font color='red'>失败</font></Button>
                : <span><font color='green'>{record.runningStatus.totalCount - record.runningStatus.unExecuteCount}/{record.runningStatus.totalCount}</font></span>
          }
        </Space>
      ),
    },
    {
      title: '操作',
      key: 'action',
      width: 150,
      render: (text, record) => (
        <Space size="middle">
          {
            record.status == 3 ?
              <Spin /> :
              <Popconfirm
                title="确定删除该任务吗?!"
                onConfirm={() => { this.deleteTask(record.id) }}
                okText="Yes"
                cancelText="No"
              >
                <a href="#">删除任务</a>
              </Popconfirm>

          }
        </Space>
      ),
    },
  ];

  detailColumns = [
    {
      title: '报表名称',
      dataIndex: 'searchName',
      key: 'searchName',
    },
    {
      title: '报表标题',
      dataIndex: 'name',
      key: 'name',
    },
    {
      title: '方案名称',
      dataIndex: 'planName',
      key: 'planName',
    },
    {
      title: '返回内容',
      key: 'result',
      render: (text, record) => (
        <Space size="middle">
          <Button type='link' onClick={() => { this.getReportResult(record.id) }}>查看内容</Button>
        </Space>
      ),
    },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
      render: (text, record) => (
        <Space size="middle">
          {text == 0 ? <Tag color='green'>成功</Tag> :
            text == 1 ? <Tag color='red'>失败</Tag> :
              text == 2 ? <Tag color='blue'>未执行</Tag> : <Tag color='pink'>正在执行</Tag>
          }
        </Space>
      ),
    },
    {
      title: '耗时',
      dataIndex: 'consumingTimes',
      key: 'consumingTimes',
    },
    {
      title: '数据量',
      dataIndex: 'rowsCount',
      key: 'rowsCount',
    },
  ];
  diffTask = [
    {
      title: '报表标题',
      dataIndex: 'leftReportTitle',
      key: 'leftReportTitle',
    },
    {
      title: '方案名称',
      dataIndex: 'leftPlanName',
      key: 'leftPlanName',
    },
    {
      title: '状态',
      dataIndex: 'leftStatus',
      key: 'status',
      render: (text, record) => (
        <Space size="middle">
          {text == 0 ? <Tag color='green'>成功</Tag> :
            text == 1 ? <Tag color='red'>失败</Tag> :
              text == 2 ? <Tag color='blue'>未执行</Tag> : <Tag color='pink'>正在执行</Tag>
          }
        </Space>
      ),
    },
    {
      title: '耗时',
      dataIndex: 'leftConsumingTime',
      key: 'leftConsumingTime',
    },
    {
      title: '报表标题',
      dataIndex: 'rightReportTitle',
      key: 'rightReportTitle',
    },
    {
      title: '方案名称',
      dataIndex: 'rightPlanName',
      key: 'rightPlanName',
    },
    {
      title: '状态',
      dataIndex: 'rightStatus',
      key: 'status',
      render: (text, record) => (
        <Space size="middle">
          {text == 0 ? <Tag color='green'>成功</Tag> :
            text == 1 ? <Tag color='red'>失败</Tag> :
              text == 2 ? <Tag color='blue'>未执行</Tag> : <Tag color='pink'>正在执行</Tag>
          }
        </Space>
      ),
    },
    {
      title: '耗时',
      dataIndex: 'rightConsumingTime',
      key: 'rightConsumingTime',
    },
    {
      title: '情况比较',
      dataIndex: 'diffTime',
      key: 'diffTime',
      render: (text, record) => (
        <Space size="middle">
          {text > 0 ? <Tag color='red'>↑{text}%</Tag>
            : <Tag color='green'>↓{-text}%</Tag>
          }
        </Space>
      ),
    },
    {
      title: '比较数据',
      key: 'diffData',
      render: (text, record) => (
        <Button onClick={() => { this.comparaData(record.searchName, record.rightPlanName,1) }}>比较数据</Button>
      ),
    },
  ];
}
