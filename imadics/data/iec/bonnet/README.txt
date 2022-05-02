This file describes IEC 104 attacks that are recorded in CSV files mentioned below. All attacks were generated on L7 layer (application). The traces were obtained by passive monitoring from the IPFIX flow probe extended with the IEC 104 plugin. The traces are stored in CSV format. 
----------
These attack traces were created in the frame of the research project Bonnet (2019-2022), see https://www.fit.vut.cz/research/project/1303/.en.

(c) Petr Matousek, 2020
Brno University of Technology
Brno, Czech Republic
Contact: matousp@fit.vutbr.cz
-----------
Attack files: 

0) normal-traffic.csv: normal IEC 104 communication (58930 packets, 2 days+19:55 hours traffic)

1) connection-loss.csv: Connection lost twice during communication
    a) connection loss from 16:27:57.68 to 16:37:48.63 (10 minutes 146 missing packets)
    b) connection loss from 08:08:01.20 to 09:08:25.95 (1 hour, 921 missing packets)

2) switching-attack.csv: Switching the device on/off
    - a series of IEC 104 packets with TypID=46 (double cmd), numix=1, CoT=6 (Act), Cot=7 (ActCon), CoT=10 (ActTerm), OA=0, Addr=655535, IOA=2
    - an attack starts at 06:27:55:00 and repeats the series (10 minutes, 72 new packets)

3) scanning-attack.csv: Horizontal (IP addresses) and vertical (IOA) scanning
    a) horizontal scanning starts at 10:32:07 and ends at 10:49:10. The attacker employs a spoofed IP address 192.168.11.102:45280. The scan sends IEC 104 U-commands TestFrame Act (ApduType 0x03, UType 0x10) on port 2404 (used by IEC 104). If a station exists, the scan yields a proper response TestFrame Conf (ApduType 0x03, UType 0x20).
    b) vertical scanning attack - explores IEC 104 information objects on the device with IP address 192.168.11.111. In order to masquerade his identity, the attacker uses a spoofed source address 192.168.11.248 which belongs to the existing node. The attacker sends an interrogation command with TypeID=100 (General Interrogation) and CoT=6 (Activation). If an object exists, it reponses with TypeID=100 and CoT=7 (Activation Conf), otherwise it sends a message with CoT=47 (unknown object address). For vertical scanning we use a default ASDU address 65535 (Global address) and a default value for the Originator address (OA = 0, not used). 
The IOA length is limited to 2 bytes, however, our scan tests only values from 1 to 127. The attack starts at 01:02:18 and ends at 01:23:19. 

4) dos-attack.csv: Denial of service attack against a IEC 104 control station. 
    - the attacker sends a hundred of legitimate IEC 104 packets to the destination. He uses a spoofed IP address 192.168.11.248 which sends an ASDU with TypeID 36 (Measured value, short floating point, with time tag) and CoT=3 (Spontaneous event). This message is only confirmed by the receiver using an APDU of the S-type. The attack start at 23:50:02 and ends at 01:18:29. It contains about 1049 spoofed messages. The attack is repeated at 02:30:05 and lasts until 04:01:54.  

5) rogue-devices.csv: A rogue devices starts communicating with an IEC 104 host using legitimate IEC 104 packets. 
    - The attacker uses a sequence of IEC 104 messages with ASDU type=36 (Measured value, short floating point with time tag) and CoT=3 (spontaneous event). It also correctly responses with supervisory APDUs. The attack start at 15:19:00 and ends at 15:46:03. It uses an IP address 192.168.11.246. The attack includes 417 packets.

6) injection-attack.cvs: An attacker compromises one host and starts sending unusual requests.
    - The attacker sends ASDUs with TypeID=45 (Single command) and CoT=6 (Activation) on the object with IOA=31, 32 and 2. The host responses with CoT=7 (Activation Conf). The attack starts at 19:35:19 and ends at 19:41:06. It includes 83 packets.
    - Another injection attack appears at 21:05:32 when an attacker starts transfering a file to the compromised host with IP address 192.168.11.111. The attacker sends messages with ASDU typeID=122 (Call directory, select file), 120 (File ready), 121 (Section ready), 123 (Last section), 124 (Ack file), 125 (Segment). The attacker accesses object with IOA=65537 which is not typically accessible. The attack includes 221 messages and ends at 21:21:14. 

 



