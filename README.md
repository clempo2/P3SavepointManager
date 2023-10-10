## P3SavepointManager

[P3SavepointManager](https://github.com/clempo2/P3SavepointManager) is a simple file manager to rename, copy or delete savepoints on the [P3 pinball machine](https://www.multimorphic.com/p3-pinball-platform/) by [Multimorphic](https://www.multimorphic.com/). This functionality is missing from the P3 feature menu in the [P3 SDK V0.8](https://www.multimorphic.com/support/projects/customer-support/wiki/3rd-Party_Development_Kit)

## Installation

These instructions show how to modify the P3SampleApp Unity project in the P3 SDK into a P3SavepointManager Unity project. This approach is necessary to satisfy the P3 SDK license. To simplify the procedure, the minimal number of steps are given. A full distribution would remove the HomeScene and lots of unused scripts.  

<pre>
  
- Download P3_SDK_V0.8.zip from the Multimorphic support site and expand it in \P3\P3_SDK_V0.8
  
- Clone the P3SavepointManager repository to a local directory named P3SM:  
    cd \P3
    git clone https://github.com/clempo2/P3SavepointManager.git P3SM  

- Copy \P3\P3_SDK_V0.8\P3SampleApp to \P3\P3SavepointManager  

- Change the app code from P3SA to P3SM:  
    Start Unity, load the project in \P3\P3SavepointManager  
    In the top menu, select Multimorphic > Rename a P3 Project  
        Enter New company name: Multimorphic  
        Enter New app code: P3SM  
        Click Continue  
    Exit Unity

- Copy the P3SavepointManager source code to the Unity project:  
    cd \P3\P3SavepointManager  
    Delete the \P3\P3SavepointManager\LauncherMedia directory inherited from P3SampleApp  
    Copy \P3\P3SM\LauncherMedia to \P3\P3SavepointManager\LauncherMedia  
    Copy \P3\P3SM\Assets\Scripts\Modes\SceneModes\P3SMAttractMode.cs to \P3\P3SavepointManager\Assets\Scripts\Modes\SceneModes\P3SMAttractMode.cs  

- Edit the Attract scene:  
    Start Unity, load the project in \P3\P3SavepointManager  
    Select Assets > Scenes in the Project Window  
    Double-click Attract to edit the Attract scene  
    Select Titles in the Hierarchy window  
        Disable the Titles: click the checkmark in the top left corner in the Inspector to remove it  
    Select ApronDisplay in the Hierarchy window  
        Disable the ApronDisplay: click the checkmark in the top left corner in the Inspector to remove it  
    Select KeyIndicators in the Hierarchy window  
        Disable the KeyIndicators: click the checkmark in the top left corner in the Inspector to remove it  
    In the top menu, select File > Save Scenes

- Repair the button legends:  
    Edit C:\P3\P3SavepointManager\Assets\Scripts\Modes\ProfileManagement\ProfileSelectorMode.cs  
    In the mode_started() method, remove this code:  
            buttonLegend["LeftWhiteButton"] = "";  
            buttonLegend["RightWhiteButton"] = "";  
            buttonLegend["LeftRedButton"] = "Previous";  
            buttonLegend["RightRedButton"] = "Next";  
            buttonLegend["LeftYellowButton"] = "Exit";  
            buttonLegend["RightYellowButton"] = "Select";  
            buttonLegend["StartButton"] = "Select";  
            buttonLegend["LaunchButton"] = "";  
    Replace it with this code:  
            buttonLegend["LeftRedButton"] = "Previous";  
            buttonLegend["RightRedButton"] = "Next";  
            buttonLegend["LeftYellowButton"] = "Exit";  
            buttonLegend["RightYellowButton"] = "Select";  
            buttonLegend["LeftWhiteButton"] = "Shift";  
            buttonLegend["RightWhiteButton"] = "Shift";  
            buttonLegend["StartButton"] = "Exit";  
            buttonLegend["LaunchButton"] = "Select";  

- Repair the button legends:  
    Edit  C:\P3\P3SavepointManager\Assets\Scripts\GUI\P3SMSceneController.cs  
    In the ShowButtonLegendEventHandler() method, locate this line  
	    buttonLegend = (GameObject)Instantiate(Resources.Load("Prefabs/Framework/ButtonLegend"));  
    Change "Prefabs/Framework/ButtonLegend" to "Prefabs/Framework/ButtonLegend3D"  

- Make P3SavepointManager module agnostic:  
    Edit C:\P3\P3SavepointManager\Assets\Scripts\Modes\P3SMBaseGameMode.cs  
    Comment out this line:  
        homeMode = new HomeMode (p3, P3SMPriorities.PRIORITY_HOME, "Home");
    Comment out this line:  
        shotsMode = new ShotsMode (p3, Priorities.PRIORITY_MECH+2);  
    Comment out this line:  
        InitBallSearchMode();  
    Comment out this line:  
        p3.AddMode (shotsMode);  

- Run P3SavepointManager in the simulator:  
    Select Assets > Scenes in the Project Window  
    Double-click on Bootstrap to load the Bootstrap scene  
    Click the Play button.  

- Build the P3SavepointManager package:  
    From the top menu, select Multimorphic > Package App For Distribution  
    Enter Display Name: P3SavepointManager  
    Enter Version: 1.0.0.0  
    Enter Description: Savepoint Manager  
    Enter Compatible Module Ids: Any  
    Leave Feature flags and Tags empty  
    Choose whether to make a copy on a USB drive  
    Click Continue
</pre>

## Support

Please submit a [GitHub issue](https://github.com/clempo2/P3SavepointManager/issues) if you find a problem.

You can discuss P3EmptyGame and other P3 Development topics on the [P3 Community Discord Server](https://discord.gg/GuKGcaDkjd) in the dev-forum channel under the 3rd Party Development section.

## License

P3SavepointManager is:  
Copyright (c) 2023 Clement Pellerin  
MIT License.  
Be aware this only covers the open-source portion of P3SavepointManager.

The P3 SDK is:  
Copyright (c) 2022 Multimorphic, Inc. All Rights Reserved
