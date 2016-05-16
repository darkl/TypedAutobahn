var connection = new autobahn.Connection({ url: "ws://127.0.0.1:8080/ws", realm: "realm1" });
connection.open();

connection.onopen = (session: autobahn.Session, details: any) => {
    let serviceProvider = new ArgumentsServiceProvider(session);

    var promise = serviceProvider.registerCallee(new ArgumentsServiceCallee());

    promise.then(x => console.log("all registered!"));

    //var proxy = serviceProvider.getCalleeProxy();

    //proxy.ping().then(() => console.log("Pinged!"));
    //proxy.add2(2, 3).then(result => console.log("Add2:", result));
    //proxy.stars().then(res => console.log("Starred 1:", res));
    //proxy.stars("Homer").then(res => console.log("Starred 2:", res));
    //proxy.stars(null, 5).then(res => console.log("Starred 3:", res));
    //proxy.stars("Homer", 5).then(res => console.log("Starred 4:", res));
    //proxy.orders("coffee");
    //proxy.orders("coffee", 10);
};