from google import genai
from google.genai import types
import sys


api_key = "KEY"
client = genai.Client(api_key=api_key)

def generate_report(transcript):

    response = client.models.generate_content(
        model="gemini-2.0-flash",
        config=types.GenerateContentConfig(
            system_instruction="You are an expert psychologist. Attached is a transcript of a psychotherapy session between a therapist and a patient. You will analyze the transcript and write a detailed observation report which should include the kind of disorders that you think the patient is suffering from, and a plan for treatment. Also record general observations / summaries that the psychologist may find helpful! NOTE: Do not use markdown formatting. State everything in plaintext, use uppercase letters for headings and sub-headings ONLY. Use normal formatting for everything else. Msake use of newlines or indents for spacing.",),
        contents=[transcript],
    )
    print(response.text);
    
if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Usage: python reportgen.py <transcript_text>", file=sys.stderr)
        sys.exit(1)
    
    transcript_text = sys.argv[1]
    generate_report(transcript_text);
    

