**RAFTiNG** is a Raft algorithm implementation in .Net.
=======================================================

Overview
========
As of now RAFTiNG is still on the early stages and I am ironing out the intended service. But the main objective is to implement trustworthy services helping developers build their clustering/load balancing/fault tolerance.

Zookeeper is a clear reference, and Raft was chosen because of the 'understandability first' approach. RAFTiNG follows that road too and aims at simplicity and understandability.

Using a RAFT implemented configuration/naming service, RARFTiNG provides the following clustering services:

1. **Single master, one or more standby**, suporting
 * Graceful stop
 * Brutal stop
 * Two steps startup (for cold startup)
1. **Farm mode**, supporting
 * Round robin load balancing
 * Monte carlo load balancing
 * Custom load balancer
1. **Multi-tenants** including usage capping to prevent any bad tenant to achieve DoS for the other

**RAFTiNG** requires as litlle configuration as possible, is administrable through a REST API.
Services relying on **RAFTiNG** for their clustering needs do not require extra configuration step as all details are provided by service nodes as they start up. API focus on a Pit of Success approach, with a happy path for the most frequent use cases.


Implementation notes
====================
1. **Assumptions**. Note that those assumptions may be revised as the implementation progress. The general intent is to reduce their number and relax them.
 * Nodes configuration is consistent; especially, the list of known nodes is the same for all nodes.
 * Persistence is atomic: current implementation assumes that corruption cannot happen: information is available and correct or is missing.
 * Middleware
  * assumption is that the middleware is hacking safe
  * it does not need to provide intrisic request reply support.
  * it may or may not provide early failure detection (think TCP RST signal).