Feature: BasicRaft
	In order to smoke test RAFT, as a developper
	I want to make sure there is only one leader at most

@mytag
Scenario: Three RAFTs allow proper initialization
	Given I have deployed 3 instances
	When I start instances 1, 2 and 3
	Then there is 0 leader

Scenario: Three RAFTs allow proper initialization after a delay
	Given I have deployed 3 instances
	When I start instances 1, 2 and 3
	And I wait 1 seconde
	Then there is 1 leader

Scenario: Five RAFTs allow proper initialization
	Given I have deployed 5 instances
	When I start all instances
	And I wait 1 seconde
	Then there is 1 leader


Scenario: Nine RAFTs allow proper initialization
	Given I have deployed 9 instances
	When I start all instances
	And I wait 1 seconde
	Then there is 1 leader

Scenario: A Lot of RAFTs still allow proper initialization
	Given I have deployed 30 instances
	When I start all instances
	And I wait 1 seconde
	Then there is 1 leader
