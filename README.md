# Detection methods for ICS networks

A library of ICS network anomaly detection methods.

## Introduction

## Environment
The individual projects can be compiled in Linux with installed dependencies. 

#### Dependencies
The solution consists of different methods that the following have dependencies:

| Package | Documentation | 
|------ | --------------------------------- |
| python36 | https://www.python.org/downloads/release/python-360/ |
| .NET 5.0 | https://docs.microsoft.com/en-us/dotnet/core/install/linux-centos | 
#### Cent OS

#### Vagrant
This is the easiest way as everything is done automatically using the Vargant file. The only requirement is to have working [Vagrant installation](https://www.vagrantup.com/downloads).

1. Create a VM described in Vagrant file:
```
vagrant up
```

2. Access the created VM:
```
vagrant ssh
```

Then everything is available in the machine - source codes, dependencies, data, necessary SDKs, etc.

#### Windows Subsystem for Linux
This method enables to develop on a host running Microsoft Windows 10 with WSL enabled. For instance, it is useful in the combination with Visual Studio Code that enables to remotely connect to the Linux instance . Contrary to Vagrant, it is not necessary to set up file sharing as this is done automatically by the operating system and WSL.

1. Setup CentOS WSL - it can be downloaded from Microsoft Store.

2. Install necessary dependencies:
```
TODO
```

3. Open WSL shell or run Visual Studio Code in WSL using the Remote WSL extension (https://code.visualstudio.com/docs/remote/wsl-tutorial). 

## Acknowledge
This project was supported by grant [VI20192022138](https://www.fit.vut.cz/research/project/1303/).
