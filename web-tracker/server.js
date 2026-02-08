const express = require('express');
const http = require('http');
const { Server } = require("socket.io");
const dgram = require('dgram');

const app = express();
const server = http.createServer(app);
const io = new Server(server, {
  cors: {
    origin: "*",
    methods: ["GET", "POST"]
  }
});
const udpClient = dgram.createSocket('udp4');

const UNITY_PORT = 12345;
const UNITY_HOST = '127.0.0.1';

app.use(express.static(__dirname));

app.get('/', (req, res) => {
  res.sendFile(__dirname + '/index.html');
});

io.on('connection', (socket) => {
  console.log('Web Client Connected');

  // OPTIMIZATION: Disable TCP Nagle's algorithm for lower latency
  if (socket.conn && socket.conn.transport && socket.conn.transport.socket) {
    if (typeof socket.conn.transport.socket.setNoDelay === 'function') {
      socket.conn.transport.socket.setNoDelay(true);
    }
  }

  socket.on('eyeData', (data) => {
    // data assumed to be { gazeX, isLeftClosed, isRightClosed }
    const jsonString = JSON.stringify(data);

    udpClient.send(jsonString, UNITY_PORT, UNITY_HOST, (err) => {
      if (err) console.error('UDP Send Error:', err);
    });
  });
});

server.listen(3000, () => {
  console.log('listening on *:3000');
  console.log('Open http://localhost:3000 in your browser');
});
