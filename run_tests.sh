#!/usr/bin/env bash
# Esegue l'intera suite headless di Controluce (più i test di rete su
# loopback) e riassume i risultati. Esce 1 al primo gruppo di FAIL.
set -u
cd "$(dirname "$0")"

GODOT="${GODOT:-godot-mono}"
PORT="${CONTROLUCE_TEST_PORT:-39555}"
FAILED=0

run() {
    local name="$1"
    if "$GODOT" --headless --path . "scenes/tests/test_$name.tscn" > /tmp/controluce_test_$name.log 2>&1; then
        echo "PASS  $name"
    else
        echo "FAIL  $name (log: /tmp/controluce_test_$name.log)"
        FAILED=1
    fi
}

echo "== Test headless =="
for t in phases rope counterweight exitzone checkpoint pull swing mechanics progression skins room04 room05 room06; do
    run "$t"
done

echo "== Test di rete (porta $PORT) =="
CONTROLUCE_MODE=server CONTROLUCE_PORT=$PORT "$GODOT" --headless --path . scenes/tests/test_net_server.tscn > /tmp/controluce_test_net_server.log 2>&1 &
SERVER_PID=$!
sleep 4
if CONTROLUCE_MODE=client CONTROLUCE_PORT=$PORT "$GODOT" --headless --path . scenes/tests/test_net_client.tscn > /tmp/controluce_test_net_client.log 2>&1; then
    echo "PASS  net_client"
else
    echo "FAIL  net_client (log: /tmp/controluce_test_net_client.log)"
    FAILED=1
fi
if wait "$SERVER_PID"; then
    echo "PASS  net_server"
else
    echo "FAIL  net_server (log: /tmp/controluce_test_net_server.log)"
    FAILED=1
fi

[ "$FAILED" -eq 0 ] && echo "Tutti i test sono verdi." || echo "Ci sono test rossi."
exit $FAILED
