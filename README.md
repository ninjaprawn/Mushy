# Mushy

Since MS2 was announced it was shutting down, I've open sourced my custom client for it, which allowed for having multiple MS2 instances on the same PC. The client currently has 4, but this limit could definitely be pushed higher (until your PC explodes :).

The core issue is this: Running the client as different users will (for some reason) allow the clients to remain alive.
This issue was discovered early last year, but I believe it came to "common knowledge" a couple months ago.

This client uses Nexon/Google logins to spawn the MS2 Client like how steam does: with the token in the command line args.

