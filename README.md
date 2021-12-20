# ICS detection algorithms software library

A library of ICS network anomaly detection methods.

## Introduction

Software library of algorithms, including their demonstration on the attached data for the detection of anomalies in ICS network traffic. The library processes input data in PCAP format and implements various methods for creating a profile of normal communication and detection of possible deviations. The library consists of methods based on various principles of communication modeling, especially automata, statistical methods, and machine learning.

The project is rather than a single software library a collection of a number of software components that implements the different anomaly detection methods that can be applied to the ICS domain. The following methods are implemented:

* [DETANO](detano) is an automated method enabling the creation of an ICS communication profile in the form of a probability automaton and uses this automaton to detect deviations from the normal profile.
* [StatProf](statistical_profiling) creates a statistical profile for ICS communication, which is used to detect differences in communication patterns. The method calculates traffic statistics based on selected properties of the monitored communication.
* [MPROF.ICS](mprof.ics) combines a set of ML-based methods (K-means, OC-SVM, PCA, Gaussian, FastTree) for profile calculation from network communication of various ICS protocols. The methods allow us to learn the profile and apply it to the observed communication.

## Environment

The individual projects can be compiled in Linux OS with installed dependencies.

### Dependencies

The solution consists of different methods that the following have dependencies:

| Package   | Documentation                                                     |
| --------- | ----------------------------------------------------------------- |
| python38  | <https://www.python.org/downloads/release/python-380/>              |
| .NET 5.0  | <https://docs.microsoft.com/en-us/dotnet/core/install/linux-ubuntu> |

### Linux Machine

This method considers to use Ubuntu 20.04 LTS, but can be modified for other Linux systems too.
The necessary dependencies can be installed using the environment installation script `setup-environment.sh`.

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

2. Create a VM and see its properties, e.g., assigned IP address:

```bash
multipass launch -n bonnet focal

multipass info bonnet
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
Additionally, it is possible to configure Visual Studio Code Remote Development (https://code.visualstudio.com/docs/remote/ssh#_getting-started). The key step is to enable SSH access to the created VM.
When VM is created Multipass generates pair of keys for SSH access. These keys are not located in user folder but in system. Depending on the OS, they
are at the following locations:

|  OS     | Path                                                                           |
| ------- | -------------------------------------------------------------------------------|
| MacOS   | `/var/root/Library/Application\ Support/multipassd/ssh-keys/id_rsa`            |
| Windows | `C:\Windows\System32\config\systemprofile\AppData\Roaming\multipassd\ssh-keys` |

1. First, test the SSH connection:

```bash
sudo ssh -i /var/root/Library/Application\ Support/multipassd/ssh-keys/id_rsa ubuntu@<VM-IP-ADDRESS>
```

2. The private key cannot be used from system location. It is necessary to copy it to .ssh folder and change the owner:

```bash
sudo cp /var/root/Library/Application\ Support/multipassd/ssh-keys/id_rsa ~/.ssh/id_rsa_bonnet
sudo chown <USER> ~/.ssh/id_rsa_bonnet
```

3. Modify the configuration file `~/.ssh/config` of SSH client by adding the following lines:

```
Host bonnet
  HostName <VM-IP-ADDRESS>
  User ubuntu
  IdentityFile ~/.ssh/id_rsa_bonnet
```

4. In Visual Studio Code it is necessary to install *Remote Development* package (https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.vscode-remote-extensionpack) and select *Remote-SSH: Connect to Host...* command to establish the connection with VM.  

5. New Visual Studio Code window is opened and connected to bonnet VM. Open folder `/mnt/bonnet` to access the project.  

## Acknowledge

This project was supported by grant [VI20192022138](https://www.fit.vut.cz/research/project/1303/).
