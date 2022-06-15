# MPC <br> Operation & Maintenance Manual

## Table of Contents
1. [Introduction](#introduction)
2. [System Components](#system-components)
      * [Randomness Client](#randomness-client)
      * [Servers](#servers)
      * [Data Client](#data-client)
3. [Running The System](#running-the-system)
4. [Maintenance](#maintenance)
     * [Error Handeling](#error-handeling)
     * [Extending Functionality](#extending-functionality)

## Introduction
Our system enables the users to perform computation on their joint data, while keeping each user's data private, this is done in a shared-secret form. The output of the computation is saved in the servers for further processes.

The application is composed of following components: 
- A randomness client that runs offline.
- Two remote servers that perform the computations.
- 1-N data clients that is in charge of getting the input from the users and send it to the servers.


## System Components

#### Randomness Client
Generates corrolated randomness for future input. Run the randomness client after both servers are on and connected, so it will be able to send them the masks and keys it generated.

#### Servers
These are the main components of the system, which are in charge of performing the computation and storing the output for further processes. 

There are two instances of the servers- A and B, and their ID is given as a program argument. For the system to run properly, the operator must first run server B because it awaits for server A to connect to it. 

#### Data Client
This is the component that runs when the users run the application. The operator doesn't need to run this component, unless he also takes part in the computation (sends his own data to the servers).

## Running The System
1. Run server B
2. Run server A
3. Run randomness client
4. Wait for inputs (data clients can now connect)

## Maintenance

#### Error Handeling

#### Extending Functionality

