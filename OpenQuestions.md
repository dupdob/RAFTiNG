**RAFTiNG Open questions**
=============================

This document list questions that should be answered in the course of the project.


**Stability**
- how can RAFTiNG handle __configuration errors__?
  When RAFTiNG is up (i.e. with one valid leader), proposition is to have leader check every new node configuration.

- how __hack resistant__ can RAFTiNG be?

**SLA**
- __in-flight command__ will eventualy by comitted if a node having them gets to be leader. How do we notify those comit to the client? Note that **GO Raft** don't care.
