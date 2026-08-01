# Nomad Rivals privacy launch checklist

This checklist records implementation findings relevant to the privacy policy and Google Play submission. It is not part of the public privacy policy.

## Required before publishing

- Replace the contact email and postal-address placeholders in `docs/privacy-policy.md`.
- Publish the policy as a normal, publicly accessible HTTPS web page. Do not use a local file, access-controlled page, editable shared document, or PDF as the Play Console URL.
- Add the same privacy-policy link inside the App, normally under **Settings > Privacy Policy**.
- Complete the Play Console **Data safety** form consistently with the App, including all data collected or shared by Firebase, AdMob, Google Play Billing, and any SDK added before release.
- Declare that the App contains ads and in-app purchases.
- Confirm the Play Console target audience. The draft policy assumes the App is **not directed to children under 13**. Do not publish that statement if children are included in the target audience without first updating the App's SDK and advertising configuration and obtaining appropriate legal review.

## Code issues found during the audit

### Account deletion is incomplete

`FirebaseManager.DeleteCurrentUserAsync()` deletes the Firebase Authentication user, and `Settings.cs` clears local PlayerPrefs. It does not delete the user's Firestore documents or leaderboard entries. This can orphan cloud data under the old user ID while the App creates a new anonymous account.

Before relying on **Settings > Delete Account** or making the policy public, implement server-authorized deletion of at least:

- `users/{uid}` and its subcollections;
- leaderboard score documents keyed by the user ID; and
- any other records keyed by or containing the user ID.

Use a trusted server or Firebase callable function for recursive deletion and verification. Also provide an external account-deletion request page/URL in Play Console, as required for apps that allow account creation.

### Ad consent is not implemented in App code

The Google User Messaging Platform package is present, but `AdsManager` initializes AdMob and requests interstitial/rewarded ads immediately. No UMP consent update, required consent form, `CanRequestAds()` gate, or privacy-options entry point was found.

Before release in the EEA, UK, Switzerland, and other applicable regions:

- configure Privacy & messaging in AdMob;
- request updated consent information on every launch;
- show a required consent form before requesting ads;
- request ads only after the SDK reports that ads may be requested; and
- add a **Privacy and cookie settings** entry point whenever required.

### Analytics control

`FirebaseManager` explicitly enables Firebase Analytics at startup and ties Analytics to the anonymous Firebase user ID. Confirm the legal basis and consent requirements for each release region. If the App offers an analytics choice, ensure collection is disabled until/when required and make the policy describe the actual control.

## Likely Play Data safety declarations (verify in the final build)

The final answers remain the developer's responsibility. Based on the repository audit, review at least these data types:

| Data category | Examples in this App | Main purposes |
|---|---|---|
| Personal info | Player name; country/region selection | Account/profile, leaderboards, game functionality |
| App activity | App launches, gameplay and race events, ad interactions | Analytics, game functionality, advertising |
| App info and performance | Diagnostics, load/performance information | Analytics, reliability, fraud prevention |
| Device or other IDs | Firebase user/installation identifiers, advertising ID, app set ID | Account management, analytics, advertising, fraud prevention |
| Approximate location | Derived from IP address by advertising/services | Advertising, analytics, fraud prevention |
| Financial info / purchase history | In-app product and transaction/purchase status handled through Google Play Billing | Purchases, account management, fraud prevention |
| Other user-generated content | Player and horse names | Profile/game functionality; player name is shown on leaderboards |

Also verify whether Google Play treats each SDK transfer as **collected**, **shared**, or eligible for a service-provider exception under the exact production configuration.

## Official references checked August 2, 2026

- Google Play User Data policy: https://support.google.com/googleplay/android-developer/answer/10144311
- Google Play Data safety guidance: https://support.google.com/googleplay/android-developer/answer/10787469
- Google Play account deletion requirements: https://support.google.com/googleplay/android-developer/answer/13327111
- Firebase Play data disclosure guidance: https://firebase.google.com/docs/android/play-data-disclosure
- Google Mobile Ads Play data disclosure: https://developers.google.com/admob/android/privacy/play-data-disclosure
- Google Mobile Ads UMP for Unity: https://developers.google.com/admob/unity/privacy

This draft is a practical compliance aid, not legal advice. Have qualified counsel review it for the countries where the App is offered.
