Feature: RaftCommunication
	As a developer I want to make sure RAFT inter node communications works properly

@Communication
Scenario: Single node heartbeat
Given I have deployed 1 instance
When I send a message to 1
Then 1 has received my message