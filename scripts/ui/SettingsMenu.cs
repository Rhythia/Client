using Godot;
using System;
using System.Collections.Generic;
using System.IO;

public partial class SettingsMenu : ColorRect
{
    public bool Shown = false;

    private Dictionary<string, Panel> settingPanels = [];
    private Button hideButton;
    private Panel holder;
    private Panel header;
    private OptionButton profilesButton;
    private VBoxContainer sidebar;
    private Panel categories;

    private ColorRect sidebarCategoryTemplate;
    private ScrollContainer categoryTemplate;

    private ScrollContainer selectedCategory;

    public override void _Ready()
    {
        hideButton = GetNode<Button>("Hide");
        holder = GetNode<Panel>("Holder");
        header = holder.GetNode<Panel>("Header");
        profilesButton = header.GetNode<OptionButton>("Profiles");
        sidebar = holder.GetNode("Sidebar").GetNode<VBoxContainer>("Container");
        categories = holder.GetNode<Panel>("Categories");

        sidebarCategoryTemplate = sidebar.GetNode<ColorRect>("SidebarCategoryTemplate");
        categoryTemplate = categories.GetNode<ScrollContainer>("CategoryTemplate");

        Modulate = Color.Color8(255, 255, 255, 0);

        SettingsManager.Instance.MenuToggled += ShowMenu;

        LineEdit profileEdit = header.GetNode<LineEdit>("ProfileEdit");

        header.GetNode<Button>("CreateProfile").Pressed += () => {
            profileEdit.Visible = !profileEdit.Visible;
        };

        profileEdit.TextSubmitted += (profile) => {
            profileEdit.Visible = false;

            SettingsManager.Save();
            SettingsManager.SetCurrentProfile(profile);
            SettingsManager.Reload();

            updateProfileSelection();
        };

        profilesButton.ItemSelected += (index) => {
            string profile = profilesButton.GetItemText((int)index);

            if (profile == SettingsManager.GetCurrentProfile()) { return; }

            SettingsManager.Save();
            SettingsManager.SetCurrentProfile(profile);
            SettingsManager.Load();
        };

        updateProfileSelection();

        Panel settingTemplate = categoryTemplate.GetNode("Container").GetNode<Panel>("SettingTemplate");
        CheckButton checkButtonTemplate = settingTemplate.GetNode<CheckButton>("CheckButton");
        HSlider sliderTemplate = settingTemplate.GetNode<HSlider>("Slider");
        LineEdit sliderLineEditTemplate = settingTemplate.GetNode<LineEdit>("SliderLineEdit");
        LineEdit lineEditTemplate = settingTemplate.GetNode<LineEdit>("LineEdit");
        OptionButton optionButtonTemplate = settingTemplate.GetNode<OptionButton>("OptionButton");
        Button buttonTemplate = settingTemplate.GetNode<Button>("Button");

        settingTemplate.Visible = false;

        foreach (Node child in settingTemplate.GetChildren())
        {
            if (child.Name == "Title") { continue; };

            settingTemplate.RemoveChild(child);
        }

        double start = Time.GetTicksUsec();

        foreach (KeyValuePair<SettingsSection, List<ISettingsItem>> section in SettingsManager.Instance.Settings.ToOrderedSectionList())
        {
            if (section.Key == SettingsSection.None) { continue; };

            string sectionName = section.Key.ToString();

            ScrollContainer category = categoryTemplate.Duplicate() as ScrollContainer;
            category.Name = sectionName;

            categories.AddChild(category);

            ColorRect sidebarCategory = sidebarCategoryTemplate.Duplicate() as ColorRect;
            Button sidebarButton = sidebarCategory.GetNode<Button>("Button");

            sidebarCategory.Name = sectionName;
            sidebarCategory.Visible = true;
            sidebarButton.Text = sectionName.ToUpper();
            sidebarButton.Pressed += () => { SelectCategory(category); };

            sidebar.AddChild(sidebarCategory);

            if (selectedCategory == null)
            {
                SelectCategory(category);
            }

            VBoxContainer container = category.GetNode<VBoxContainer>("Container");

            foreach (ISettingsItem setting in section.Value)
            {
                Panel panel = settingTemplate.Duplicate() as Panel;
                panel.Name = setting.Id;
                panel.Visible = true;

                Label title = panel.GetNode<Label>("Title");
                title.Text = setting.Title;
                title.TooltipText = setting.Description;

                if (setting.Type == typeof(bool))
                {
                    CheckButton checkButton = checkButtonTemplate.Duplicate() as CheckButton;

                    setupToggle(setting, checkButton);
                    panel.AddChild(checkButton);
                }
                else if (setting.Slider != null)
                {
                    HSlider slider = sliderTemplate.Duplicate() as HSlider;
                    LineEdit lineEdit = sliderLineEditTemplate.Duplicate() as LineEdit;

                    setupSlider(setting, slider, lineEdit);
                    panel.AddChild(slider);
                    panel.AddChild(lineEdit);
                }
                else if (setting.Type == typeof(string) && setting.List == null)
                {
                    LineEdit lineEdit = lineEditTemplate.Duplicate() as LineEdit;

                    setupInput(setting, lineEdit);
                    panel.AddChild(lineEdit);
                }
                else if (setting.List != null)
                {
                    OptionButton optionButton = optionButtonTemplate.Duplicate() as OptionButton;

                    setupList(setting, optionButton);
                    panel.AddChild(optionButton);
                }

                if (setting.Type == typeof(Variant))
                {
                //     Button button = buttonTemplate.Duplicate() as Button;

                //     setupButton(setting, button);
                //     panel.AddChild(button);
                }

                container.AddChild(panel);
                settingPanels[setting.Id] = panel;
            }

            settingTemplate.QueueFree();
        }

        Logger.Log($"SETTINGS MENU: {(Time.GetTicksUsec() - start) / 1000}ms");

        ShowMenu(false);

        hideButton.Pressed += () => { ShowMenu(false); };
    }
    // Adding GetViewport().SetInputAsHandled() will prevent the Quit popup from appearing when clicking ESC in settings
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey eventKey && eventKey.Pressed)
        {
            switch (eventKey.Keycode)
            {
                case Key.O:
                    if (eventKey.CtrlPressed) { ShowMenu(!Shown); GetViewport().SetInputAsHandled(); }
                    break;
                case Key.Escape:
                    if (Shown) { ShowMenu(false); GetViewport().SetInputAsHandled(); }
                    break;
            }

        }
    }

	public void ShowMenu(bool show)
	{
        Shown = show;
        hideButton.MouseFilter = show ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;

        CallDeferred("move_to_front");

        if (Shown)
		{
            Visible = true;
            holder.OffsetTop = 25;
            holder.OffsetBottom = 25;
        }

        Tween tween = CreateTween().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out).SetParallel();
        tween.TweenProperty(this, "modulate", Color.Color8(255, 255, 255, (byte)(Shown ? 255 : 0b0)), 0.25);
        tween.TweenProperty(holder, "offset_top", Shown ? 0 : 25, 0.25);
		tween.TweenProperty(holder, "offset_bottom", Shown ? 0 : 25, 0.25);
        tween.Chain().TweenCallback(Callable.From(() => { Visible = Shown; }));
    }

	public void SelectCategory(ScrollContainer category)
	{
        if (selectedCategory != null)
        {
            sidebar.GetNode<ColorRect>(new(selectedCategory.Name)).Color = Color.Color8(255, 255, 255, 0);
            selectedCategory.Visible = false;
        }

        selectedCategory = category;

        selectedCategory.Visible = true;
		sidebar.GetNode<ColorRect>(new(selectedCategory.Name)).Color = Color.Color8(255, 255, 255, 8);
    }

    private void updateProfileSelection()
    {
        // skip default
        for (int i = 1; i < profilesButton.ItemCount; i++)
        {
            profilesButton.RemoveItem(i);
        }

        string current = SettingsManager.GetCurrentProfile();
        string[] profiles = Directory.GetFiles($"{Constants.USER_FOLDER}/profiles");

        for (int i = 0; i < profiles.Length; i++)
        {
            string name = profiles[i].GetFile().GetBaseName();

            if (name != "default")
            {
                profilesButton.AddItem(name);
            }

            if (current == name)
            {
                profilesButton.Select(i);
            }
        }
    }

    private void setupToggle(ISettingsItem setting, CheckButton button)
	{
        button.Toggled += value => {
            if ((bool)setting.GetVariant() != value) { setting.SetVariant(value); }
        };

        setting.Updated += (value) => { updateToggle(button, (bool)value); };

        updateToggle(button, (bool)setting.GetVariant());
    }

	private void updateToggle(CheckButton button, bool value)
	{
        button.ButtonPressed = value;
    }

	private void setupSlider(ISettingsItem setting, HSlider slider, LineEdit lineEdit)
	{
		void applyLineEdit()
        {
            double value = (lineEdit.Text == "" ? lineEdit.PlaceholderText : lineEdit.Text).ToFloat();

            if ((double)setting.GetVariant() != value) { setting.SetVariant(value); }
        }

        // lineEdit.PlaceholderText = ((SettingsItem<Variant>)setting).DefaultValue.ToString();
        slider.Step = setting.Slider.Step;
        slider.MinValue = setting.Slider.MinValue;
        slider.MaxValue = setting.Slider.MaxValue;

        lineEdit.FocusExited += applyLineEdit;
        lineEdit.TextSubmitted += (_) => { applyLineEdit(); };
        slider.ValueChanged += value => {
            if ((double)setting.GetVariant() != value) { setting.SetVariant(value); }
        };

        setting.Updated += (value) => { updateSlider(slider, lineEdit, (double)value); };

        updateSlider(slider, lineEdit, (double)setting.GetVariant());
    }

	private void updateSlider(HSlider slider, LineEdit lineEdit, double value)
	{
        lineEdit.Text = value.ToString();

        if (lineEdit.IsInsideTree())
        {
            lineEdit.ReleaseFocus();
        }

        slider.SetValueNoSignal(value);
    }

    private void setupInput(ISettingsItem setting, LineEdit lineEdit)
    {
        void applyLineEdit()
        {
            string value = (lineEdit.Text == "" ? lineEdit.PlaceholderText : lineEdit.Text);

            if ((string)setting.GetVariant() != value) { setting.SetVariant(value); }
        }

        lineEdit.FocusExited += applyLineEdit;
        lineEdit.TextSubmitted += (_) => { applyLineEdit(); };

        setting.Updated += (value) => { updateInput(lineEdit, (string)value); };

        updateInput(lineEdit, (string)setting.GetVariant());
    }

    private void updateInput(LineEdit lineEdit, string input)
    {
        lineEdit.Text = input;

        if (lineEdit.IsInsideTree())
        {
            lineEdit.ReleaseFocus();
        }
    }

    private void setupList(ISettingsItem setting, OptionButton optionButton)
    {
        foreach (Variant item in setting.List.Values)
        {
            optionButton.AddItem((string)item);
        }

        optionButton.ItemSelected += (id) => {
            string oldVal = (string)setting.GetVariant();
            string newVal = (string)setting.List.Values[(int)id];

            if (oldVal != newVal) { setting.SetVariant(newVal); }
        };

        int getIndex()
        {
            int index = 0;

            foreach (string value in setting.List.Values)
            {
                if (value == (string)setting.List.SelectedValue)
                {
                    break;
                }

                index++;
            }

            return index;
        }

        setting.Updated += (_) => { updateList(optionButton, getIndex()); };

        updateList(optionButton, getIndex());
    }

    private void updateList(OptionButton optionButton, int index)
    {
        optionButton.Selected = index;
    }

    // private void setupButton(ISettingsItem setting, Button button)
    // {

    // }
}
