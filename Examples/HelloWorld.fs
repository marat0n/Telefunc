module HelloWorld

open Telefunc

let sayHello name =
    Wrappers.sendMessages(
        
    )

runBot
    "TOKEN"
    [
        Filter.command "start" [ sayHelloWorld ]
        Filter.message [
            Filter.includeText "Hello" [ sayHelloToUser ]
        ]
    ]
