package selfevolving

default allow := true

deny contains reason if {
    some namespace in data.Namespaces
    namespace == "System.IO"
    reason := sprintf("Restricted namespace: %s", [namespace])
}

deny contains reason if {
    some namespace in data.Namespaces
    namespace == "System.Net"
    reason := sprintf("Restricted namespace: %s", [namespace])
}

deny contains reason if {
    some namespace in data.Namespaces
    namespace == "System.Reflection"
    reason := sprintf("Restricted namespace: %s", [namespace])
}

deny contains reason if {
    some namespace in data.Namespaces
    namespace == "System.Runtime.InteropServices"
    reason := sprintf("Restricted namespace: %s", [namespace])
}

deny contains reason if {
    some call in data.MethodCalls
    startswith(call, "System.IO.File")
    reason := sprintf("Restricted invocation: %s", [call])
}

deny contains reason if {
    some call in data.MethodCalls
    startswith(call, "System.IO.Directory")
    reason := sprintf("Restricted invocation: %s", [call])
}

deny contains reason if {
    some call in data.MethodCalls
    startswith(call, "System.Reflection.Assembly")
    reason := sprintf("Restricted invocation: %s", [call])
}

deny contains reason if {
    some call in data.MethodCalls
    startswith(call, "System.Runtime.InteropServices.Marshal")
    reason := sprintf("Restricted invocation: %s", [call])
}

allow if count(deny) == 0
