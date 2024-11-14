
var signalValuesCache = [0, 0];
signal some_signal(int a, int b);

some_signal += (int a, int b) : int => {
    signalValuesCache[0] += a;
    signalValuesCache[1] += b;
    
    print('Received signal: {0}, {1} -> signalValuesCache = {0}', a, b, signalValuesCache);
};

for(var i = range 4) {
    some_signal.emit(1, 1);
}

print('Emitted signals, signalValuesCache = {0}', signalValuesCache);