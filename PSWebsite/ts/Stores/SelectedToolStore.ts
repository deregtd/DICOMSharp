import { StoreBase, AutoSubscribeStore, autoSubscribe, key } from 'resub';

@AutoSubscribeStore
class SelectedToolStoreImpl extends StoreBase {
    private _toolMap: { [button: number]: Tool } = {
        [MouseButton.Left]: Tool.Scroll,
        [MouseButton.Middle]: Tool.Pan,
        [MouseButton.Wheel]: Tool.Scroll,
        [MouseButton.Right]: Tool.WindowLevel
    };

    setButtonTool(button: MouseButton, tool: Tool) {
        if (this._toolMap[button] !== tool) {
            this._toolMap[button] = tool;
            this.trigger(button);
        }
    }

    @autoSubscribe
    getButtonTool(@key button: MouseButton): Tool {
        return this._toolMap[button];
    }
}

export = new SelectedToolStoreImpl();
