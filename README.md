

---

# **MxNet C\# (SIMPL\#) Integration for Crestron**

This project provides a robust C\# (SIMPL\#) library and a set of SIMPL+ wrappers to control an MxNet AV-over-IP system from a Crestron 4-Series processor. It communicates directly with the MxNet "CBox" controller via TCP/IP.

## **🏛️ Project Architecture**

This solution is built on a modern Crestron C\# architecture, separating the logic and communication from the SIMPL program.

* **C\# Core Library (TSI.MXNet.clz):** A SIMPL\# library that handles all the heavy lifting.  
  * **Cbox.cs (Singleton):** This is the core "brain" of the integration. It uses a **singleton pattern** (CBox.Instance) to ensure only one connection to the CBox is ever created. It manages the TCP client, parses all incoming JSON responses, and maintains the device lists and routing status.  
  * **MxnetDecoderClass.cs:** This class represents a single decoder. Each instance of the SIMPL+ decoder module creates an instance of this class, which then registers itself with the main CBox singleton to receive relevant feedback and send commands.  
  * **TcpClientAsync.cs:** A modern, asynchronous TCP client that manages the connection, automatically handles disconnects and reconnects, and queues outgoing commands.  
* **SIMPL+ Wrappers (.usp):** These modules act as the "bridge" between your SIMPL program and the C\# library.  
  * **TSI \- MXNet Cbox Processor \- IP \- v1.0.usp:** The main controller module. You only need **one** of these in your program.  
  * **TSI \- MXnet Decoder Client.usp:** The decoder client module. You use **one of these for each decoder** you need to control.

## **🚀 How to Use / Implementation Guide**

Follow these steps to integrate the MxNet system into your Crestron program.

1. **Compile and Load C\#:**  
   * Compile the C\# project (which includes Cbox.cs, MxnetDecoderClass.cs, TcpClientAsync.cs, etc.) to get the TSI.MXNet.clz library file.  
   * Add the TSI.MXNet.clz to your SIMPL program's "UserSIMPL\#" folder.  
   * You must also add any dependencies, such as **Newtonsoft.Json.dll**, to the UserSIMPL\# folder, as this library is used for parsing responses.  
2. **Add SIMPL+ Modules:**  
   * Add both TSI \- MXNet Cbox Processor \- IP \- v1.0.usp and TSI \- MXnet Decoder Client.usp to your SIMPL project.  
3. **Step 1: Configure the CBox Processor (Main Module)**  
   * Drag **one (1)** instance of TSI \- MXNet Cbox Processor \- IP \- v1.0.usp into your program's Logic folder.  
   * In the module's parameters, set the **cb\_Ipaddress** and **cb\_Port** (default is 24\) 1 of your MxNet CBox.

   * Pulse the **InitializeCbox** input *after* Program\_Start (e.g., from a "System Initializing" signal).  
   * The **IsCommunicating** output will go high when a TCP connection is established2.

   * The **InitializationComplete** output will go high after the module has successfully connected and received the device list from the CBox3.

4. **Step 2: Configure the Decoder Clients**  
   * Drag **one (1)** instance of TSI \- MXnet Decoder Client.usp into your program for **each decoder** you wish to control.  
   * On each module instance, set the **DecoderID** string input. This *must* match the device ID from the MxNet system (e.g., "01Decoder").  
     * *Tip:* You can use the DecoderID\[64\] string outputs on the Cbox Processor module to see the list of available IDs discovered from the system.  
   * **Wait** for the Cbox Processor's InitializationComplete signal to go high.  
   * Once InitializationComplete is high, pulse the **Initialize** input on *each* Decoder Client module. This registers the client with the C\# CBox singleton.  
5. **Step 3: Control and Feedback**  
   * You can now control routing by sending a 1-based source index to the **route** analog input on a Decoder Client module.  
   * The **Route\_Fb** analog output will provide the 1-based feedback for the currently routed source4.

   * You can also control the stream status (on/off) using the **StreamOn** and **StreamOff** inputs5555. Feedback is provided on **StreamOn\_Fb**6.

## **📋 SIMPL+ Module API Reference**

### **TSI \- MXNet Cbox Processor \- IP \- v1.0.usp**

This is the main module for connecting to the CBox. **Use only one instance.**

| Type | Name | Description |
| :---- | :---- | :---- |
| **Parameter** | cb\_Ipaddress\[15\] | The IP address or hostname of the MxNet CBox\. |
| **Parameter** | cb\_Port | The TCP port for the CBox (default is 24\).  |
| **Input** | Debug | (D) Set high to enable detailed trace messages in the console\. |
| **Input** | InitializeCbox | (D) Pulse to connect to the CBox and initialize the singleton\. |
| **Input** | GetDeviceList | (D) Pulse to manually request an updated device list\. |
| **Input** | CommandToSend\[256\] | (S) Send a raw command string directly to the CBox\. |
| **Input** | cbox\_ipaddress$\[15\] | (S) An alternate way to set the IP address via a serial signal. |
| **Input** | SwitchDecoder\[64\] | (A) Array input to route a source (value) to a decoder (index)\. |
| **Input** | DecoderStreamOff\[64\] | (D) Array input to turn a decoder's stream off (index)\. |
| **Output** | Info | (S) Provides general info responses from the CBox\. |
| **Output** | Cmd | (S) Provides the last command sent or received\. |
| **Output** | Error | (S) Provides the last error message received\. |
| **Output** | IsCommunicating | (D) High when connected to the CBox TCP socket.  |
| **Output** | InitializationComplete | (D) High when connected *and* the device list has been received.  |
| **Output** | ErrorReceived | (D) Pulses when an error message is received from the CBox\. |
| **Output** | EncoderID\[32\] | (S) Array of discovered Encoder IDs. |
| **Output** | DecoderID\[64\] | (S) Array of discovered Decoder IDs\. |
| **Output** | Route\_fb\[64\] | (A) Array feedback of 1-based source routed to each decoder (index)\. |

---

### **TSI \- MXnet Decoder Client.usp**

This is the client module. **Use one instance per decoder.**

| Type | Name | Description |
| :---- | :---- | :---- |
| **Parameter** | switchType\[2\] | Sets the switch type (e.g., "a", "v", "av", "u", "z" for all)\. |
| **Input** | Initialize | (D) Pulse *after* CBox is initialized to register this decoder. |
| **Input** | StreamOn | (D) Pulse to turn the decoder's stream on\. |
| **Input** | StreamOff | (D) Pulse to turn the decoder's stream off.  |
| **Input** | route | (A) The 1-based index of the source (encoder) to route to this decoder. |
| **Input** | videopathdisable | (D) Pulse to disable the video path for this decoder\. |
| **Input** | DecoderID\[16\] | (S) The unique ID string for this specific decoder. **(Required)** |
| **Output** | Route\_Fb | (A) The 1-based index of the source currently routed to this decoder.  |
| **Output** | Source\_Id$ | (S) The string ID of the source currently routed to this decoder.  |
| **Output** | StreamOn\_Fb | (D) High when the decoder's stream is active.  |

---

