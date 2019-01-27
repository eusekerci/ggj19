var app = require('express')();
var http = require('http').Server(app);
var io = require('socket.io')(http);

app.get('/', function(req, res){
  res.sendFile(__dirname + '/index.html');
});

var userId = 0;
var userCount = 0;

io.on('connection', function(socket){
  socket.userId = userId ++;
  userCount++;
  console.log('user id: ' + socket.userId + ' Connected // User Count: ' + userCount);
  socket.broadcast.emit('conpack', {
    id: socket.userId,
    count: userCount,
    msg: "Connected"
  });
  socket.on('chat', function(msg){
    console.log('message from user#' + socket.userId + ": " + msg);
    socket.broadcast.emit('chat', {
      id: socket.userId,
      msg: msg
    });
  });
  socket.on('disconnect', function () {
    userCount--;
    console.log('user id: ' + socket.userId + ' Disconnected // User Count: ' + userCount);
    socket.broadcast.emit('conpack', {
      id: socket.userId,
      count: userCount,
      msg: "Disconnected"
    });
  });
});

http.listen(31319, function(){
  console.log('listening on *:31319');
});