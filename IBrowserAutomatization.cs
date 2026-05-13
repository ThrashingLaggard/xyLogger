public interface IBrowserAutomatizationMessages : IAbsoluteMessageProvider,
    IFrameSwitchingMessages, IFrameSwitchingMessages, IResultStatusCodeMessages,
    IModalDialogMessages, IWaitingConditionsMessages, IScrollingMessages,
    IDragAndDropMessages, IAlertMessages, IJavaScriptMessages, ITabSwitchingMessages,
    ICookieMessages, IElementLookupAndValidationMessages, IFormSubmissionMessages,
    IUploadMessages, IDownloadMessages, IScreenshotMessages, IEnterStuffMessages,
    IClickingMessages, INavigationMessages
{

}
public interface IFrameSwitchingMessages : IAbsoluteMessageProvider
{
    public string FrameSwitch(By by);

    public string SwitchedToDefaultContent();

    public string FrameNotFound(By by);

    public string FrameSwitchFailed(By by);

}
public interface IResultStatusCodeMessages : IAbsoluteMessageProvider
{
    public string ActionSuccess(string description);

    public string ActionFailed(string description);

    public string HttpStatusCode(int statusCode);

    public string OperationCompleted(string name);

}
public interface IModalDialogMessages : IAbsoluteMessageProvider
{
    public string ModalDetected(By by);
    public string ModalNotFound(By by);

    public string ModalHandled(By by);
    public string ModalHandlingFailed(By by)
        }
public interface IWaitingConditionsMessages : IAbsoluteMessageProvider
{
    public string WaitUntilVisible(By by, int seconds);

    public string WaitUntilClickable(By by, int seconds);

    public string WaitConditionFail(By by, string condition);

}
public interface IScrollingMessages : IAbsoluteMessageProvider
{
    public string ScrolledToElement(By by);

    public string ScrollToElementFailed(By by);

    public string ScrolledByOffset(int x, int y);
}
public interface IDragAndDropMessages : IAbsoluteMessageProvider
{
    public string DragAndDropSuccess(By source, By target);

    public string DragAndDropFail(By source, By target);

}
public interface IAlertMessages : IAbsoluteMessageProvider
{
    public string AlertPresent(string text);
    public string AlertNotPresent();

    public string AlertAccepted();
    public string AlertDismissed();

    public string AlertHandlingFailed(string reason);

}
public interface IJavaScriptMessages : IAbsoluteMessageProvider
{
    public string JsExecuted(string description);

    public string JsExecutionFailed(string description);

    public string JsReturnedNull(string description);

    public string JsReturnedValue(string description, string value);
}
public interface ITabSwitchingMessages : IAbsoluteMessageProvider
{
    public string TabSwitched(int index);

    public string TabSwitchFailed(int index);

    public string TabClosed(int index);

}
public interface ICookieMessages : IAbsoluteMessageProvider
{
    public string CookieAdded(string name);
    public string CookieDeleted(string name);
    public string CookieRetrieved(string name, string value);
    public string CookieNotFound(string name);
    public string CookieOperationFailed(string name, string action);
}
public interface IElementLookupAndValidationMessages : IAbsoluteMessageProvider
{
    public string ElementVisible(By by);
    public string ElementInvisible(By by);

    public string ElementClickable(By by);
    public string ElementNotClickable(By by):

            public string ElementMissing(By by);
    public string ElementFound(By by);
    public string ElementNotFound(By by);

    public string ElementValidationPass(By by);
    public string ElementValidationFail(By by);

    public string ElementAttributeMismatch(By by, string attr, string expected, string actual);
}
public interface IFormSubmissionMessages : IAbsoluteMessageProvider
{
    public string FormSubmitStart(By by);

    public string FormSubmitSuccess(By by);

    public string FormSubmitFail(By by);

    public string FormSubmitTimeout(By by, int seconds);
}
public interface IUploadMessages : IAbsoluteMessageProvider
{
    public string UploadStart(string filePath);

    public string UploadSuccess(string filePath);

    public string UploadFail(string filePath);

    public string UploadElementNotFound(By by);
}
public interface IDownloadMessages : IAbsoluteMessageProvider
{
    public string DownloadStarted(string url);
    public string DownloadTimeout(string url, int seconds);

    public string DownloadSuccess(string filePath);
    public string DownloadFail(string url);
}
public interface IScreenshotMessages : IAbsoluteMessageProvider
{
    public string ScreenshotSuccess(string filePath);
    public string ScreenshotFail(Exception ex);

}
public interface IEnterStuffMessages : IAbsoluteMessageProvider
{
    public string EnterPassword(By by, string tag, string type);

    public string EnterText(By by, string input, string tag, string type);

    public string EnterSkipButton(By by, string type);

    public string EnterTimeout(By by, int seconds);

    public string EnterStale(By by);

    public string EnterUnexpected(By by);

    public string EnterFail(By by);

}
public interface IClickingMessages : IAbsoluteMessageProvider
{
    public string ClickSuccess(string textOrTag);
    public string ClickFail(By by);

    public string ClickTimeout(By by, int seconds);
    public string ClickStale(By by);
    public string ClickUnexpected(By by);
}
public interface INavigationMessages : IAbsoluteMessageProvider
{
    public string NavigationStart(string url);
    public string NavigationTimeout(string url, int seconds);

    public string NavigationSuccess(string url);
    public string NavigationUnexpected(string url);

}