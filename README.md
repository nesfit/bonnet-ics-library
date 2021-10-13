# Detection methods for ICS networks

A library of ICS network anomaly detection methods.

## Introduction

This project

## Environment

The individual projects can be compiled in Linux with installed dependencies.

### Dependencies

The solution consists of different methods that the following have dependencies:

| Package   | Documentation                                                     |
| --------- | ----------------------------------------------------------------- |
| python36  | <https://www.python.org/downloads/release/python-360/>              |
| .NET 5.0  | <https://docs.microsoft.com/en-us/dotnet/core/install/linux-ubuntu> |

### Linux Machine

This method considers to use Ubuntu 20.04 LTS, but can be modified for other Linux systems too.
The necessary dependencies can be installed using the environment installation script `setup-environment.sh`.
The required packages are:

* .NET 5.0
* Python 3.6

### Windows Subsystem for Linux (WSL2) with Visual Studio Code

This method enables to develop on a host running Microsoft Windows 10 with WSL enabled. For instance, it is useful in the combination with Visual Studio Code that enables to remotely connect to the Linux instance . Contrary to Vagrant, it is not necessary to set up file sharing as this is done automatically by the operating system and WSL.

1. Setup WSL2 and install Ubuntu 20.04 LTS (<https://wiki.ubuntu.com/WSL>)

3. Install necessary dependencies:

```
TODO
```

3. Open WSL shell or run Visual Studio Code in WSL using the Remote WSL extension (<https://code.visualstudio.com/docs/remote/wsl-tutorial>).

### Multipass

Alternatively, it is possible to use Multipass, which provides a virtual Linux environment suitable for development and testing. Steps:

1. Install multipass for your OS (<https://multipass.run/>).

2. Create a VM using the following command:

```bash
multipass launch -n bonnet focal
```

3. Setup sharing project folder on a host with VM:

```bash
multipass mount <PROJECT_FOLDER> bonnet:/mnt/bonnet
```

4. Connect to VM and go to the project folder:

```bash
multipass shell bonnet
```

```bash
cd /mnt/bonnet
```

5. Execute environment setup script:

```bash
chmod a+x setup-environment.sh
./setup-environment.sh
```

When completing all the previous steps, the environment is prepared to compile the library. The project is mounted in the VM at folder `/mnt/bonnet`.

## Acknowledge

This project was supported by grant [VI20192022138](https://www.fit.vut.cz/research/project/1303/).
