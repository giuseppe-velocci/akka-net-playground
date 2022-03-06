# How to run
Start project **AkkaNetPlayground** to start an Actor with scheduled call to a Stream that try to get a response from a local api endpoint. 
It will fail gracefully.

Then start **DummyServerApi** project to get the correct response.

# Test Akka remote
Start project **AkkaNetPlayground** , then start **AkkaRemoteCaller**. This project will send a remote message to *apiActor* created in the first project.
It will log a message like this: "Received message from Remote sender <sender info>".