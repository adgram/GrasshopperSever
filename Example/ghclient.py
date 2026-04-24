"""
TCP 客户端标准方式
"""
import socket, json, threading, time
from datetime import datetime
from typing import Optional, Callable, Any
import re



class GHClient:
    """Grasshopper 服务器 TCP 客户端"""
    def __init__(self, host='127.0.0.1', port=6879, timeout=10):
        self.host, self.port, self.timeout = host, port, timeout
        self.client: Optional[socket.socket] = None
        self.connected: bool = False
        self.receive_thread: Optional[threading.Thread] = None
        self.receive_callback: Optional[Callable[[dict], None]] = None
        self.running: bool = False
        self.lock = threading.Lock()

    def connect(self, retry_count=3, retry_delay=2) -> list[dict]:
        '''建立 TCP 连接 - 会收到一条消息
        Args:
            retry_count: 连接失败时的重试次数
            retry_delay: 重试间隔时间（秒）
            Returns:
                list[dict]: 接收到的消息列表
        '''
        for attempt in range(retry_count):
            try:
                self.client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
                self.client.settimeout(self.timeout)
                self.client.connect((self.host, self.port))
                self.connected = True
                return self.receive(max_count=1)  # 连接后接收 1 条消息
            except Exception as e:
                print(f"连接失败 (尝试 {attempt + 1}/{retry_count}): {e}")
                if attempt < retry_count - 1:
                    time.sleep(retry_delay)
        return []

    def disconnect(self):
        self.running = False
        if self.receive_thread:
            self.receive_thread.join(timeout=1)
            self.receive_thread = None
        with self.lock:
            if self.client:
                try: self.client.close()
                except: pass
                self.client = None
        self.connected = False

    def send(self, name: str, info: str, value: Any, max_count=10) -> list[dict]:
        '''发送消息并接收响应
        Args:
            name: 消息名称
            info: 消息信息
            value: 消息值
            max_count: 最大接收消息数量
        Returns:
            list[dict]: 接收到的消息列表
        '''
        if not self.connected or not self.client:
            return []
        try:
            data = {"Name": name, "Info": info, "Time": datetime.now().isoformat(), "Value": value}
            message = json.dumps(data, ensure_ascii=False)
            with self.lock:
                self.client.sendall((message + '\n').encode('utf-8'))
            return self.receive(max_count=max_count)  # 发送后接收 max_count 条消息
        except Exception as e:
            print(f"发送失败: {e}")
            return []

    def send_msg(self, name: str, info: str, value: Any) -> list[dict]:
        return self.send(name, info, value, max_count=1)

    def send_command(self, name: str, info: str, value: dict) -> list[dict]:
        return self.send(name, info, value, max_count=2)

    def receive(self, timeout=None, max_count=10) -> list[dict]:
        '''按行接收服务器响应（使用 readline()）
        Args:
            timeout: 超时时间（秒）
            max_count: 最大接收消息数量
        Returns:
            list[dict]: 接收到的消息列表
        '''
        if not self.connected or not self.client:
            return []
        if timeout:
            self.client.settimeout(timeout)
        reader = self.client.makefile('r', encoding='utf-8-sig')
        messages = []
        for i in range(max_count):
            try:
                line = reader.readline()
                if not line:
                    break
                line = line.strip()
                if not line:
                    continue
                # 尝试解析 JSON
                try:
                    msg = json.loads(line)
                    messages.append(msg)
                except json.JSONDecodeError as e:
                    # 尝试去除 BOM 标记
                    if line.startswith('\ufeff'):
                        try:
                            msg = json.loads(line[1:])
                            messages.append(msg)
                        except json.JSONDecodeError as e2:
                            print(f"去除 BOM 后仍失败：{e2}")
                    else:
                        print(f"JSON 解析失败：{e}")
            except Exception as e:
                print(f"接收消息时出错：{e}")
                break
        
        reader.close()
        return messages

    def start_receive_thread(self, callback: Callable[[dict], None]):
        self.receive_callback = callback
        self.running = True
        def receive_loop():
            while self.running and self.connected and self.client:
                try:
                    responses = self.receive(timeout=1)
                    for response in responses:
                        if self.receive_callback and self.running:
                            self.receive_callback(response)
                except: break
        self.receive_thread = threading.Thread(target=receive_loop, daemon=True)
        self.receive_thread.start()

    def stop_receive_thread(self):
        self.running = False
        if self.receive_thread:
            self.receive_thread.join(timeout=2)
            self.receive_thread = None

    def __enter__(self):
        self.connect()
        return self

    def __exit__(self, *args):
        self.disconnect()

    @staticmethod
    def extract_value(response_text: list[dict], key):
        """从响应中提取指定键的值"""
        for r in response_text:
            if "Value" in r and isinstance((value := r["Value"]), dict):
                return value.get(key, None)
        return None

    @staticmethod
    def scriptvariable_param(variableName, **kwargs):
        '''
        需要创建一个json
        必须参数：
        variableName:string 
        可选参数：
        typeHintName:string = "No Type Hint";
        showTypeHints:bool = true;
        allowTreeAccess:bool = true;
        toolTip:string = "";
        scriptParamAccess:int = 0;
        optional:bool = false;
        hidden:bool = false;
        description:string = "";
        castTargetType:string = "";
        '''
        return {"Name": variableName, **kwargs}


if __name__ == '__main__':
    with GHClient(port=6655) as client:
        responses = client.send_command(
            name="COMPONENT",
            info="通过名称查找组件",
            value={
                "Command": "FINDCOMPONENTBYNAME",
                "Name": "Find similar member"
            }
        )
        print(responses)