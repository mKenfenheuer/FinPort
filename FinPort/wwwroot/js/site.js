
var connect = function () {
    const socket = new WebSocket('/api/websocket');

    // Connection opened
    socket.addEventListener('open', function (event) {
        console.log('WebSocket connection established');
    });

    // Listen for messages
    socket.addEventListener('message', function (event) {
        var data = JSON.parse(event.data);
        var target = $('[data-type="' + data.Type + '"][data-field="' + data.Field + '"][data-id="' + data.Id + '"]')
        
        if(target.length == 0) return;

        var valueLocal = Math.round(data.Value * 100) / 100;
        var unit = data.Field == "value" ? "€" : "%";
        var current = parseFloat(target.attr("data-value"));
        if (data.Value > current) {
            target.addClass('text-success');
            target.removeClass('text-danger');
        }
        else if (data.Value < current) {
            target.remove('text-success');
            target.addClass('text-danger');
        }
        else {
            target.removeClass('text-success');
            target.removeClass('text-danger');
        }
        target.html(valueLocal.toLocaleString() + " " + unit);
        target.attr('data-value', data.Value);
        target.addClass('pulse');
        setTimeout(() => {
            target.removeClass('pulse');
        }, 1000);
    });

    // Connection closed
    socket.addEventListener('close', function (event) {
        console.log('WebSocket connection closed');
    });

    // Error occurred
    socket.addEventListener('error', function (event) {
        console.error('WebSocket error:', event);
    });
}
connect();