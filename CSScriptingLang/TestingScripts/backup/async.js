
// var server = new<HttpServer>('127.0.0.1:6969');
// print('is listening? {0}', server.isListening);
// server.listen();
// print('is listening? {0}', server.isListening);

async function sleep(int ms) {
    //print('sleeping for {0} ms', ms);
    await Async.sleep(ms);
//    print('done sleeping');
}

print('pre await');
await sleep(200);
print('post await');

async function runA() {
    var i = 0;
    for {
        print('running A: {0}', i);
        await sleep(1000);
        i++;
        
        if(i > 5) {
            break;
        }
    }
}
async function runB() {
    var i = 0;
    for {
        print('running B: {0}', i);
        await sleep(500);
        i++;
        
        if(i > 15) {
            break;
        }
    }
}

var a = runA();
var b = runB();

a.run();
b.run();

 await a;
// await b;


/*

async function listenLoop() {
    for {
        print('listening...');
        
        sleep(1000);
    
        */
/*if(!server.isListening) {
            break;
        }*//*

    }
}

async function spamLoop() {
    for {
        print('spammus...');        
        Async.sleep(500);
    }
}

listenLoop();
spamLoop();

*/
